import cntk as C
import numpy
import sys
import random
from operator import itemgetter
import pdb

#condition = [1, 3, 5, 6]
#condition = [0, 1, 2, 7, 8, 9, 10, 11, 12, 15, 16, 17, 18, 20]
special_mark = [',', '?', '!', ':', ';', '[', ']', '(', ')', '-', '+', '*', '%', '/', '\\']
#special_mark = ['.', ',', '?', '!', ':', ';', '[', ']', '(', ')', '-', '+', '*', '%', '/', '\\']
# Read the mapping of tokens to ids from a file (tab separated)

def split_text(line):
    ret = []
    line = line.strip()
    if len(line) == 0:
        return ret
    space = [-1]
    in_brace = False
    cnt = 0
    while cnt < len(line):
        ch = line[cnt]
        if ch == '{':
            in_brace = True
            cnt += 1
            continue
        elif ch == '}':
            in_brace = False
            cnt += 1
            continue			
        if ch == ' ' and in_brace == False:
            space.append(cnt)
        cnt += 1
    space.append(len(line))
    cnt = 0
    while cnt + 1 < len(space):
        part = line[space[cnt] + 1: space[cnt + 1]].strip()
        if len(part) > 0:
            ret.append(part) 
        cnt += 1
    return ret
	
def load_token_to_id(lines):
    id_to_token = {}
    token_to_id = {}
    token_all = {}
    cnt = 0
    while cnt < len(lines):
        temp = lines[cnt].strip()
        #for mark in special_mark:
        #    temp = temp.replace(mark, ' ' + mark)
        #words = temp.split()
        words = split_text(temp)
        
        for e in words:
            e = e.strip()
            if e in token_all:
                token_all[e] += 1
            else:
                token_all[e] = 1
        cnt += 1

    token_dic = sorted(token_all.items(), key=itemgetter(1), reverse=True)
    token_to_id['<unk>'] = 0
    id_to_token[0] = '<unk>'
    token_to_id['<eos>'] = 1
    id_to_token[1] = '<eos>'
    token_to_id['<bos>'] = 2
    id_to_token[2] = '<bos>'
    word_cnt = 3
    for e in token_dic:
        token_to_id[e[0].strip()] = word_cnt
        id_to_token[word_cnt] = e[0].strip()
        #fw_token.write(e[0].strip() + '\t' + str(word_cnt) + '\n')
        word_cnt += 1
    vocab_dim = len(token_to_id)
    
    return token_to_id, id_to_token, vocab_dim

def split(line):
    ret = []
    line = line.strip()
    if len(line) == 0:
        return ret
    comma = [-1]
    in_quotation = False
    cnt = 0
    while cnt < len(line):
        ch = line[cnt]
        if ch == '\"' and cnt > 0 and line[cnt - 1] == '=' \
            or ch == '\"' and cnt + 1 < len(line) and line[cnt + 1] == ',' \
            or ch == '\"' and cnt + 1 == len(line):
            in_quotation = not in_quotation
        if ch == ',' and in_quotation == False:
            comma.append(cnt)
        cnt += 1
    comma.append(len(line))
    cnt = 0
    while cnt + 1 < len(comma):
        part = line[comma[cnt] + 1: comma[cnt + 1]].strip()
        if len(part) > 0:
            ret.append(part) 
        cnt += 1
    return ret

def split_train_valid(lines, sv_lines):
    size = len(lines)
    r = [i for i in range(size)]
    random.shuffle(r)
    split = size / 10.0
    train = []
    valid = []
    slot_value = []
    valid_slot_value = []
    for i in range(len(r)):
        if i < split:
            valid.append(lines[r[i]])
            valid_slot_value.append(sv_lines[r[i]])
        else:
            train.append(lines[r[i]])
            slot_value.append(sv_lines[r[i]])

    return train, valid, slot_value, valid_slot_value

def load_slot_value_to_id(lines):
    svpair_all = {}
    slot_all = {}
    cnt = 0
    while cnt < len(lines):
        line = lines[cnt].strip()
        pairs = split(line)
        j = 0
        slot_in_one_sent = {}
        while j < len(pairs):
            if len(pairs[j].strip()) == 0:
                j += 1
                continue
            slot = '{' + pairs[j].split('=')[0].strip() + '}'
            idx = pairs[j].index('=')
            value = pairs[j][idx + 1:].strip()
            value = value.replace('\"', '')
            if len(value.strip()) == 0:
                j += 1
                continue
            slot_all[slot] = 1 
            if slot.find('(@C)') != -1:
                #sv = pairs[j].lower().replace('\"', '').strip()
                sv = slot + '=' + value
            else:
                if slot not in slot_in_one_sent:
                    sv = slot + '=' + '_1'
                    slot_in_one_sent[slot] = 1
                else:
                    sv = slot + '=' + '_' + str(slot_in_one_sent[slot] + 1)
                    slot_in_one_sent[slot] += 1
            
            if sv not in svpair_all:
                dic_size = len(svpair_all)
                svpair_all[sv] = dic_size
            j += 1
        cnt += 1

    return slot_all, svpair_all, len(svpair_all)

def get_slot_value(line, svpair_all):
    line = line.strip()
    ret = []
    no_pair = {}
    pairs = split(line)
    j = 0
    while j < len(pairs):
        if len(pairs[j].strip()) == 0:
            j += 1
            continue
        slot = '{' + pairs[j].split('=')[0].strip() + '}'
        idx = pairs[j].index('=')
        value = pairs[j][idx + 1:].strip()
        value = value.replace('\"', '')
        if len(value.strip()) == 0:
            j += 1
            continue
        #sv = pairs[j].lower().replace('\"', '').strip()
        sv = slot + '=' + value
        if sv in svpair_all:
            ret.append(svpair_all[sv])
        else:
            if slot in no_pair:
                no_pair[slot] += 1
            else:
                no_pair[slot] = 1
        j += 1
    for k,v in no_pair.items():
        sv = k + '=_' + str(v)
        if sv in svpair_all:
            ret.append(svpair_all[sv])
    return ret

def load_vocab(vocab_file):
    id_to_token = {}
    token_to_id = {}
    with open(vocab_file,'r') as f:
       for line in f:
            entry = line.split('\t')
            if len(entry) == 2:
                id_to_token[int(entry[1])] = entry[0]
                token_to_id[entry[0]] = int(entry[1])

    return token_to_id, id_to_token, len(id_to_token)
    
def load_slot_value(slot_value_file):
    svpair_all = {}
    with open(slot_value_file,'r') as f:
       for line in f:
            entry = line.strip().split('\t')
            if len(entry) == 2:
                svpair_all[entry[0]] = int(entry[1])

    return svpair_all, len(svpair_all)

# Provides functionality for reading text file and converting them to mini-batches using a token-to-id mapping from a file.
class DataReader(object):
    def __init__(self,
        text_lines,
        slotvalue_lines,
        segment_begin_token, # segment separator
        segment_end_token, # segment separator
        vocab_file = '',
        slotvalue_file = ''
                ):
        if len(vocab_file) == 0: 
            self.token_to_id, self.id_to_token, self.vocab_dim = load_token_to_id(text_lines)
            self.slot_all, self.svpair_all, self.sv_dim = load_slot_value_to_id(slotvalue_lines)
            self.vocab_dim = len(self.token_to_id)
            
            self.segment_begin_id = self.token_to_id[segment_begin_token]
            self.segment_end_id = self.token_to_id[segment_end_token]
        else:
            self.token_to_id, self.id_to_token, self.vocab_dim = load_vocab(vocab_file)
            self.svpair_all, self.sv_dim = load_slot_value(slotvalue_file)

            self.segment_begin_id = self.token_to_id[segment_begin_token]
            self.segment_end_id = self.token_to_id[segment_end_token]
            

    # Creates a generator that reads the whole input file and returns mini-batch data as a triple of input_sequences, label_sequences and number of read tokens.
    def minibatch_generator(
        self,
        text_lines,
        slot_value_lines,
        sequence_length,     # Minimal sequence length
        sequences_per_batch, # Number of sequences per batch
        hidden_dim,               
        sv_dim                    
        ):

        sv_sequence = []
        sv_sequences_tmp = []
        inputH = []
        inputC = []

        # read slot value pair file, convert to one hot vector
        #with open(sv_path) as svpair_file:
        for line in slot_value_lines:
            tmp = get_slot_value(line, self.svpair_all)
            sv_sequences_tmp.append(tmp)

        for line in sv_sequences_tmp:
            sv = numpy.zeros(sv_dim) 
            for e in line:
                if e >= sv_dim:
                    e = sv_dim - 1
                sv[e] = 1.0
            sv_sequence.append(sv)
            hh = numpy.zeros(hidden_dim)
            inputH.append(hh)
            inputC.append(hh)

        #with open(input_text_path) as text_file: 
        token_ids = []
        feature_sequences = []
        label_sequences = []
        token_count = 0
        cnt = 0 
        sv_input = []
        hh_input = []
        cc_input = []

        # hh_input, cc_input are initial state of h and c
        for line in text_lines:
            #tokens = line.split()
            tokens = split_text(line)
            if len(token_ids) == 0:
                token_ids.append(self.segment_begin_id)

            for token in tokens:
                if not token in self.token_to_id:
                    #print ("ERROR: token without id: " + token)
                    # it is an unknown word
                    token_ids.append(0)
                else:
                    token_ids.append(self.token_to_id[token])

            # add an end to the sentence
            token_ids.append(self.segment_end_id)
            sv_input.append(sv_sequence[cnt])
            hh_input.append(inputH[cnt])
            cc_input.append(inputC[cnt])
            cnt += 1
            token_count += len(tokens)

            feature_sequences.append(token_ids[ : -1])
            label_sequences.append(token_ids[ 1 :])
            token_ids = []
            # When the expected number of sequences per batch is reached yield the data and reset the array
            if len(feature_sequences) == sequences_per_batch:
                feature_onehot = []
                label_onehot = []
                for e in feature_sequences:
                    feature_onehot.append(numpy.eye(self.vocab_dim, dtype=numpy.float32)[e])
                for e in label_sequences:
                    label_onehot.append(numpy.eye(self.vocab_dim, dtype=numpy.float32)[e])

                    # convert input to numpy.array to speed up
                hh_input = numpy.array(hh_input, dtype=numpy.float32)
                cc_input = numpy.array(cc_input, dtype=numpy.float32)
                sv_input = numpy.array(sv_input, dtype=numpy.float32)

                yield feature_onehot, sv_input, hh_input, cc_input, label_onehot, token_count
                feature_sequences = []
                label_sequences   = []
                sv_input   = []
                hh_input   = []
                cc_input   = []
                token_count = 0

            # From the end of the file there are probably some leftover lines
        if len(feature_sequences) > 0:
            feature_onehot = []
            label_onehot = []
            for e in feature_sequences:
                feature_onehot.append(numpy.eye(self.vocab_dim, dtype=numpy.float32)[e])
            for e in label_sequences:
                label_onehot.append(numpy.eye(self.vocab_dim, dtype=numpy.float32)[e])

            # convert input to numpy.array to speed up
            hh_input = numpy.array(hh_input, dtype=numpy.float32)
            cc_input = numpy.array(cc_input, dtype=numpy.float32)
            sv_input = numpy.array(sv_input, dtype=numpy.float32)
                
            yield feature_onehot, sv_input, hh_input, cc_input, label_onehot, token_count

if __name__=='__main__':
    pass

