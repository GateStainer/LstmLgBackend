import numpy as np
import os
import cntk as C
import timeit
from cntk import Axis
from cntk import hardmax
from cntk.train import Trainer
from cntk.learners import momentum_sgd, momentum_as_time_constant_schedule, learning_rate_schedule, UnitType
from cntk.ops import sequence
from cntk.losses import cross_entropy_with_softmax
from cntk.metrics import classification_error
from cntk.ops.functions import load_model
from cntk.ops import slice
from cntk.layers import Sequential, For, RecurrenceFrom, Dense
from cntk.logging import log_number_of_parameters, ProgressPrinter
from data_reader import DataReader, split_train_valid
from math import log, exp
from cntk.device import try_set_default_device, cpu, gpu
from cntk.variables import Constant, Parameter
from cntk.layers.typing import Tensor, Sequence
from cntk.ops.functions import CloneMethod, load_model, Function
from cntk.layers.typing import *
from beamSearchNode import *
from queue import PriorityQueue
from sclstm import LSTM
from Parser import LSTMParser
import operator
import pdb
import sys

# Creates model subgraph computing cross-entropy with softmax.
def cross_entropy_with_full_softmax(
    output,  # Node providing the output of the lstm layers
    target_vector,  # Node providing the expected labels
    sv_dim, 
    vocab_dim
    ):
    sv_vector = output.outputs[3]
    z = output.outputs[0]
    zT = C.times_transpose(z, target_vector)
    # cross entropy loss with softmax function
    ce = - C.log(zT)
    # the error 
    zMax = C.reduce_max(z)
    error = C.less(zT, zMax)
    ce = sequence.reduce_sum(ce)
    # discourages the network from turning more than one gate off in a single time step.
    sumc = C.abs(C.sequence.slice(sv_vector, 1, 0) - C.sequence.slice(sv_vector, 0, -1))
    sumc = sequence.reduce_sum(0.0001 * C.pow(100.0, sumc))
    #ce += sumc
    # penalise generated utterances that failed to render all the required slots
    sumc += C.abs(C.sequence.last(sv_vector))
    sumc += C.abs(C.sequence.first(sv_vector) - output.outputs[4])
    sumc = C.reduce_sum(sumc)
    ce = C.reduce_sum(ce)
    ce += sumc
    return ce, error

# Creates model lstm model
def create_model(hidden_dim, sv_dim, vocab_dim):
    # config embedding layer, semantically conditioned lstm layer, and output layer
    with C.layers.default_options(enable_self_stabilization=True):
        emb_in = C.layers.Embedding(hidden_dim)
        lstm_in = LSTM(hidden_dim, sv_dim)
        proj_in = C.layers.Dense(vocab_dim)
    @C.Function
    def sclstm(input, inputH, inputC, svpair):
        emb = emb_in(input)
        #initial state of lstm, h0, c0, sv0
        initial_state = (inputH, inputC, svpair)
        latent_vector = RecurrenceFrom(lstm_in, go_backwards=False, return_full_state=True)(*(initial_state + (emb,)))
        #output vector with vocab dimension
        output_vector = proj_in(latent_vector.outputs[0])
        output_vector = C.softmax(output_vector)
        return output_vector, latent_vector.outputs[0], latent_vector.outputs[1], latent_vector.outputs[2], svpair
 
    return sclstm

# Creates model inputs
def create_inputs(hidden_dim, sv_dim, vocab_dim):
    input_seq_axis = Axis('inputAxis')
    input_sequence = sequence.input_variable(shape=vocab_dim, sequence_axis=input_seq_axis)
    # state in model, including sv vector, h vector, c vector
    sv_pair = C.input_variable(shape=sv_dim)
    inputH = C.input_variable(shape=hidden_dim)
    inputC = C.input_variable(shape=hidden_dim)
    label_sequence = sequence.input_variable(shape=vocab_dim, sequence_axis=input_seq_axis)
    
    return input_sequence, sv_pair, label_sequence, inputH, inputC

def logSumExp(p):
    ret = 0.0
    if len(p) == 0 or len(p[0]) == 0:
        return ret
    maxData = np.max(p[0][0])
    for e in p[0][0]:
        ret += np.exp(e - maxData)
    ret = maxData + np.log(ret)
    return ret

# beam search: 
# use priority queue, every step, get a node with highest score, expand n(beam width) states to the next step, and push them into the queue
# loop this until there are m(overgen) results generated
# backtrace the m results
def beamsearch(root, vocab_dim, length=100, x=None, hh=None, cc=None, sv_pair=None, id_to_token=None, overgen = 10, beamsize=10, begin_sentence_id=2, end_sentence_id=1):

    def gen(p, n):
        w = np.argsort(p, axis=2)[0,0][-n :][::-1]
        prob = np.sort(p, axis=2)[0,0][-n :][::-1]
            
        return w, prob

    # loop through length of generated text, sampling along the way
    endnodes = []
    # the node has h, c, sv state, pre node, the id of current word, and score
    node = BeamSearchNode(hh, cc, sv_pair, None, x, begin_sentence_id, 0, 1)
    nodes = PriorityQueue()
    # push the score and node into the queue 
    nodes.put((-node.eval(), node))
    qsize = 1

    while True:
        if qsize > 5000: break
        if nodes.qsize() == 0: break
        # get the top node and its score
        score, n = nodes.get()
        if n.wordid == end_sentence_id and n.prevNode != None:
            n.score -= np.sum(abs(np.array(n.sv)))
            endnodes.append((-n.eval(), n))
            if len(endnodes) >= overgen:
                break
            else:
                continue

        p, h, c, sv, t = root(n.wordvec, n.h, n.c, n.sv) 
        # generate n next states 
        words, probs = gen(p, beamsize)
        for i in range(len(words)):
            x = np.zeros((1, vocab_dim), dtype=np.float32)
            x[0, words[i]] = 1
            # compute the score, push into queue
            node = BeamSearchNode(h, c, sv, n, x, words[i], n.score + np.log(probs[i])  - np.sum(0.0001 *(100.0 ** abs(np.array(sv) - np.array(n.sv)))), n.leng + 1)
            nodes.put((-node.eval(), node))
        qsize += len(words)

    if len(endnodes) == 0:
        for n in range(overgen):
            if nodes.qsize() > 0:
                endnodes.append(nodes.get())
            
    utts = []
    for score, n in sorted(endnodes, key=operator.itemgetter(0)):
        utt = [n.wordid]
        while n.prevNode != None:
            # back trace
            n = n.prevNode
            utt.append(n.wordid)

        utt = utt[::-1][1:-1]
        result = ' '.join([id_to_token[e]  for e in utt])
        utts.append(result)

    return utts

def init_trainer(config, text_lines, slot_value_lines):

    hidden_dim = config.hidden_dim

    segment_begin = config.segment_begin
    segment_end = config.segment_end

    data = DataReader(text_lines, slot_value_lines, segment_begin, segment_end)

    # Create model nodes for the source and target inputs
    vocab_dim = data.vocab_dim
    sv_dim = data.sv_dim

    input_sequence, sv_pair, label_sequence, inputH, inputC = create_inputs(hidden_dim, sv_dim, vocab_dim)
    model = create_model(hidden_dim, sv_dim, vocab_dim)
    z = model(input_sequence, inputH, inputC, sv_pair)
    # cross_entropy: this is used training criterion
    ce, err = cross_entropy_with_full_softmax(z, label_sequence, sv_dim, vocab_dim)

    learning_rate = config.learning_rate
    momentum_as_time_constant = config.momentum_as_time_constant
    clipping_threshold_per_sample = config.clipping_threshold_per_sample
    lr_schedule = learning_rate_schedule(learning_rate, UnitType.sample)
    gradient_clipping_with_truncation = True
    momentum_schedule = momentum_as_time_constant_schedule(momentum_as_time_constant)
    # Instantiate the trainer object to drive the model training
    learner = momentum_sgd(z.parameters, lr_schedule, momentum_schedule,
			gradient_clipping_threshold_per_sample=clipping_threshold_per_sample,
			gradient_clipping_with_truncation=gradient_clipping_with_truncation)
    trainer = Trainer(z, (ce, err), learner)
    inputs = [input_sequence, sv_pair, label_sequence, inputH, inputC]

    return data, z, trainer, inputs

def save_vocab_slot(token_to_id, svpair_all, token_to_id_path, slot_value_path):
    with open(token_to_id_path, 'w') as f:
        for k, v in token_to_id.items():
            f.write(k + '\t' + str(v) + '\n')

    with open(slot_value_path, 'w') as f:
        for k, v in svpair_all.items():
            f.write(k + '\t' + str(v) + '\n')

def early_stop(config, text_lines, slot_value_lines, valid_text_lines = '', valid_slot_value_lines = ''):

    hidden_dim = config.hidden_dim

    sequence_length = config.sequence_length
    sequences_per_batch = config.sequences_per_batch
    num_epochs = config.num_epochs
    patience = config.patience

    data, z, trainer, inputs = init_trainer(config, text_lines, slot_value_lines)

    # can this be overflow ???
    history_err = []
    earlystop = False
    bad_count = 0
    minibatch_cnt = 0

    for epoch_count in range(num_epochs):
        for features, svs, hh, cc, labels, token_count in data.minibatch_generator(text_lines, slot_value_lines, sequence_length, sequences_per_batch, hidden_dim, data.sv_dim):
           
            arguments = ({inputs[0] : features, inputs[3]:hh, inputs[4]:cc, inputs[1]: svs, inputs[2]: labels})
            trainer.train_minibatch(arguments)
            
            # early stop
            if minibatch_cnt % 10 == 0:

                err_v = 0.0
                if len(valid_text_lines) > 0:
                    for features, svs, hh, cc, labels, token_count in data.minibatch_generator(valid_text_lines, valid_slot_value_lines, sequence_length, sequences_per_batch, hidden_dim, data.sv_dim):
                        err_v += trainer.test_minibatch(arguments)
                else:
                    err_v = trainer.test_minibatch(arguments)

                if len(history_err) > 0 and err_v < np.array(history_err).min():
                    bad_count = 0
            
                if len(history_err) > patience and err_v >= \
                    np.array(history_err)[:-patience].min():
                    bad_count += 1
                    if bad_count > patience:
                        earlystop = True
                        break
                
                history_err.append(err_v)

            minibatch_cnt += 1

        if earlystop == True:
            break

    return minibatch_cnt

def train_valid(config, all_lines, slot_value_all_lines):

    text_lines, valid_text_lines, slot_value_lines, valid_slot_value_lines = split_train_valid(all_lines, slot_value_all_lines)

    minibatch_cnt = early_stop(config, text_lines, slot_value_lines, valid_text_lines, valid_slot_value_lines)
 
    return minibatch_cnt

def train_no_valid(config, all_lines, slot_value_all_lines):

    minibatch_cnt = early_stop(config, all_lines, slot_value_all_lines)
 
    return minibatch_cnt

def n_fold_train_valid(config, all_lines, slot_value_all_lines):

    minibatch_final = 0
    for fold in range(len(all_lines)):
        text_lines = []
        valid_text_lines = []
        slot_value_lines = []
        valid_slot_value_lines = []
  
        for i in range(len(all_lines)):
            if i != fold:
                text_lines.append(all_lines[i])
                slot_value_lines.append(slot_value_all_lines[i])
            else:
                valid_text_lines.append(all_lines[i])
                valid_slot_value_lines.append(slot_value_all_lines[i])

        minibatch_cnt = early_stop(config, text_lines, slot_value_lines, valid_text_lines, valid_slot_value_lines)
        minibatch_final += minibatch_cnt

    return int(minibatch_final / len(all_lines))

# Creates and trains a semantically conditioned lstm language model.
def train_lm(config):

    token_to_id_path = config.token_to_id_path
    slot_value_path = config.slot_value_path
    hidden_dim = config.hidden_dim

    sequence_length = config.sequence_length
    sequences_per_batch = config.sequences_per_batch
    num_epochs = config.num_epochs

    train_file_path = config.train_file_path
    sv_file_path = config.sv_file_path
    
    f = open(train_file_path)
    fsv = open(sv_file_path)
    all_lines = []
    slot_value_all_lines = []

    for line in f.readlines():
        all_lines.append(line.strip())

    for line in fsv.readlines():
        slot_value_all_lines.append(line.strip())

    f.close()
    fsv.close()

    train_size = len(all_lines)

    t_start = timeit.default_timer()

    if train_size >= 10:
        minibatch_final = train_valid(config, all_lines, slot_value_all_lines)
    elif train_size < 10 and train_size > 1:
        minibatch_final = n_fold_train_valid(config, all_lines, slot_value_all_lines) 
    else:
        minibatch_final = train_no_valid(config, all_lines, slot_value_all_lines) 
        
    print("number of iteration: " + str(minibatch_final))
    minibatch_cnt = 0

    data, z, trainer, inputs = init_trainer(config, all_lines, slot_value_all_lines)

    for epoch_count in range(num_epochs):
        for features, svs, hh, cc, labels, token_count in data.minibatch_generator(all_lines, slot_value_all_lines, sequence_length, sequences_per_batch, hidden_dim, data.sv_dim):
           
            arguments = ({inputs[0] : features, inputs[3]:hh, inputs[4]:cc, inputs[1]: svs, inputs[2]: labels})
            trainer.train_minibatch(arguments)
            minibatch_cnt += 1
            if minibatch_cnt == minibatch_final:
                break

        if minibatch_cnt == minibatch_final:
            break

    model_file_path = config.model_file_path
    z.save(model_file_path)
    save_vocab_slot(data.token_to_id, data.svpair_all, token_to_id_path, slot_value_path)

    t_end =  timeit.default_timer()
    print("time: " + str(t_end - t_start) + " sec")

# test a semantically conditioned lstm language model.
def test_lm(config):
    model_file_path = config.model_file_path
    model = load_model(model_file_path)
    result_file = open('result.txt', 'w')

    token_to_id_path = config.token_to_id_path
    slot_value_path = config.slot_value_path
    segment_begin = config.segment_begin
    segment_end = config.segment_end

    test_file_path = config.test_file_path
    sv_test_file_path = config.sv_test_file_path
    with open(test_file_path,'r') as f:
        test_lines = f.readlines()
    with open(sv_test_file_path,'r') as f:
        sv_test_lines = f.readlines()

    sequence_length = config.sequence_length
    sequences_per_batch = config.sequences_per_batch
    hidden_dim = config.hidden_dim
    overgen = config.overgen
    beamwidth = config.beamwidth
    decode = config.decode 

    data = DataReader('', '', segment_begin, segment_end, token_to_id_path, slot_value_path)
    begin_sentence_id = data.segment_begin_id
    end_sentence_id = data.segment_end_id
    id_to_token = data.id_to_token 

    vocab_dim = data.vocab_dim
    sv_dim = data.sv_dim
    
    for features, svs, hh, cc, labels, token_count in data.minibatch_generator(test_lines, sv_test_lines, sequence_length, sequences_per_batch, hidden_dim, sv_dim):
        cnt = 0
        while cnt < len(features):
            # just input the first vector, begin sentence vector
            fe1 = [np.array([features[cnt][0].tolist()], dtype=np.float32)]
            sv1 = [np.array([svs[cnt].tolist()], dtype=np.float32)]
            hh1 = [np.array([hh[cnt].tolist()], dtype=np.float32)]
            cc1 = [np.array([cc[cnt].tolist()], dtype=np.float32)]
            if decode == 'beam':
                result = beamsearch(model, vocab_dim, 100, fe1, hh1, cc1, sv1, id_to_token, overgen, beamwidth, begin_sentence_id, end_sentence_id)
              
            for res in result:
                result_file.write(res.strip() + '\n')
            cnt += 1
    result_file.close()

if __name__=='__main__':
    config = LSTMParser()
    id = sys.argv[1]
    config.token_to_id_path = str(id) + "/token.txt"
    config.slot_value_path = str(id) + "/slot_value.txt"
    config.train_file_path = str(id) + "/text.train"
    config.sv_file_path = str(id) + "/sv_train.txt"
    config.model_file_path = str(id) + "/lm.dnn"
    # train the LM
    train_lm(config)
    # test the LM
    #test_lm(config)
