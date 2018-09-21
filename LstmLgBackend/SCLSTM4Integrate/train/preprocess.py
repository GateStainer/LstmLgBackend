from operator import itemgetter
import pdb
condition = [0, 1, 2, 7, 8, 9, 10, 11, 12, 15, 16, 17, 18, 20]
#condition = [1, 3, 5, 6]
#condition = []
#f = open('timezone_train.txt')
#ft = open('timezone_test.txt')
#f = open('stock_train.txt')
#ft = open('stock_test.txt')
#fv = open('stock_test.txt')
f = open('sports_train.txt')
ft = open('sports_test.txt')
fv = open('sports_test.txt')
fw_train = open('text.train', 'w')
fw_train_sv = open('svpair.train', 'w')
fw_token = open('token.txt', 'w')
fw_sv_dict = open('sv_dict.txt', 'w')
fw_valid = open('text.valid', 'w')
fw_valid_sv = open('svpair.valid', 'w')
fw_test = open('text.test', 'w')
fw_test_sv = open('svpair.test', 'w')
lines = f.readlines()
cnt = 0
svpair_all = {}
token_all = {}
slot_all = {}

# train: generate token file and template file
while cnt < len(lines):
  line = lines[cnt].strip()
  temp = lines[cnt + 1].strip()
  temp = temp.replace(',', ' ,')
  temp = temp.replace('.', ' .')
  pairs = line.split(',')
  j = 0
  slot_in_one_sent = {}
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    slot = '{' + pairs[j].split('=')[0].strip() + '}'
    #slot = pairs[j].split('=')[0].strip().lower()
    idx = pairs[j].index('=')
    value = pairs[j][idx + 1:].strip()
    value = value.replace('\"', '')
    temp = (' '+temp+' ').replace(' '+value+' ',' '+slot+' ',1)[1:-1]
    slot_all[slot] = 1 
    if j in condition:
      sv = slot + "=" + value
      #sv = pairs[j].lower().replace('\"', '').strip()
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
  #pdb.set_trace()
  temp = temp[0].lower() + temp[1:]
  fw_train.write(temp + '\n')
  #fw_train.write(temp.lower() + '\n')
  words = temp.split()
  #words = temp.lower().split()
  for e in words:
    e = e.strip()
    if e in token_all:
      token_all[e] += 1
    else:
      token_all[e] = 1

  cnt += 3

word_cnt = 3
fw_token.write('<unk>\t0\n')
fw_token.write('<eos>\t1\n')
fw_token.write('<bos>\t2\n')
 
token_dic = sorted(token_all.items(), key=itemgetter(1), reverse=True)

for e in token_dic:
  fw_token.write(e[0].strip() + '\t' + str(word_cnt) + '\n')
  word_cnt += 1

# generate slot value pair dictionary
for sv,v in svpair_all.items():
  fw_sv_dict.write(sv + '\t' + str(v) + '\n')

fw_sv_dict.close()

# train: generate slot value pair file
cnt = 0  
while cnt < len(lines):
  line = lines[cnt].strip()
  pairs = line.split(',')
  j = 0
  svpairs = []
  slot_in_one_sent = {}
  sv_in_one_sent = {}
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    if j in condition:
      slot = pairs[j].split('=')[0].strip()
      idx = pairs[j].index('=')
      value = pairs[j][idx + 1:].strip()
      value = value.replace('\"', '')
      sv = slot + "=" + value
      #sv = pairs[j].lower().replace('\"', '').strip()
      #if sv in svpair_all:
      #  svpairs.append(str(svpair_all[sv]))
      if sv in svpair_all:
        svpairs.append(sv)
    else:
      slot = pairs[j].split('=')[0].strip()
      #slot = pairs[j].split('=')[0].strip().lower()
      if slot not in slot_in_one_sent:
        sv = slot + '=' + '_1'
        slot_in_one_sent[slot] = 1
        sv_in_one_sent[sv] = 1
      else:
        sv = slot + '=' + '_' + str(slot_in_one_sent[slot] + 1)
        slot_in_one_sent[slot] += 1
        sv_in_one_sent[sv] = 1
    j += 1
  
  for sv in sv_in_one_sent:
    if sv in svpair_all:
      svpairs.append(str(sv))
      #svpairs.append(str(svpair_all[sv]))
    
  res = ','.join(svpairs)
  fw_train_sv.write(res + '\n')
  cnt += 3

fw_train.close()
fw_token.close()
fw_train_sv.close()

# test: generate template file 
lines = ft.readlines()
cnt = 0
while cnt < len(lines):
  line = lines[cnt].strip()
  temp = lines[cnt + 1].strip()
  temp = temp.replace(',', ' ,')
  temp = temp.replace('.', ' .')
  pairs = line.split(',')
  j = 0
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    slot = pairs[j].split('=')[0]
    idx = pairs[j].index('=')
    value = pairs[j][idx + 1:]
    value = value.replace('\"', '')
    temp = (' '+temp+' ').replace(' '+value+' ',' '+slot.upper()+' ',1)[1:-1]
    j += 1
  temp = temp[0].lower() + temp[1:]
  fw_test.write(temp + '\n')
  cnt += 3

# test: generate slot value pair file
cnt = 0  
while cnt < len(lines):
  line = lines[cnt].strip()
  pairs = line.split(',')
  j = 0
  svpairs = []
  sv_in_one_sent = {}
  slot_in_one_sent = {}
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    if j in condition:
      sv = pairs[j].lower().replace('\"', '').strip()
      if sv in svpair_all:
        svpairs.append(str(svpair_all[sv]))
    else:
      slot = pairs[j].split('=')[0].strip().lower()
      if slot not in slot_in_one_sent:
        sv = slot + '=' + '_1'
        slot_in_one_sent[slot] = 1
        sv_in_one_sent[sv] = 1
      else:
        sv = slot + '=' + '_' + str(slot_in_one_sent[slot] + 1)
        slot_in_one_sent[slot] += 1
        sv_in_one_sent[sv] = 1
    j += 1

  for sv in sv_in_one_sent:
    if sv in svpair_all:
      svpairs.append(str(svpair_all[sv]))

  res = ','.join(svpairs)
  fw_test_sv.write(res + '\n')
  cnt += 3

fw_test.close()
fw_test_sv.close()

# valid: generate template file 
lines = fv.readlines()
cnt = 0
while cnt < len(lines):
  line = lines[cnt].strip()
  temp = lines[cnt + 1].strip()
  temp = temp.replace(',', ' ,')
  temp = temp.replace('.', ' .')
  pairs = line.split(',')
  j = 0
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    slot = pairs[j].split('=')[0]
    idx = pairs[j].index('=')
    value = pairs[j][idx + 1:]
    value = value.replace('\"', '')
    temp = (' '+temp+' ').replace(' '+value+' ',' '+slot.upper()+' ',1)[1:-1]
    j += 1
  fw_valid.write(temp.lower() + '\n')
  cnt += 3

# test: generate slot value pair file
cnt = 0  
while cnt < len(lines):
  line = lines[cnt].strip()
  pairs = line.split(',')
  j = 0
  svpairs = []
  sv_in_one_sent = {}
  slot_in_one_sent = {}
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    if j in condition:
      sv = pairs[j].lower().replace('\"', '').strip()
      if sv in svpair_all:
        svpairs.append(str(svpair_all[sv]))
    else:
      slot = pairs[j].split('=')[0].strip().lower()
      if slot not in slot_in_one_sent:
        sv = slot + '=' + '_1'
        slot_in_one_sent[slot] = 1
        sv_in_one_sent[sv] = 1
      else:
        sv = slot + '=' + '_' + str(slot_in_one_sent[slot] + 1)
        slot_in_one_sent[slot] += 1
        sv_in_one_sent[sv] = 1
    j += 1

  for sv in sv_in_one_sent:
    if sv in svpair_all:
      svpairs.append(str(svpair_all[sv]))

  res = ','.join(svpairs)
  fw_valid_sv.write(res + '\n')
  cnt += 3

fw_valid.close()
fw_valid_sv.close()

f.close()
fv.close()
ft.close()
