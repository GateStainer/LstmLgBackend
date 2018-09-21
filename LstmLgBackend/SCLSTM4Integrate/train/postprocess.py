#fres = open('text.test')
fres = open('../result.txt')
ftest = open('stock_test.txt')
ffinal = open('final.txt', 'w')
lineres = fres.readlines()
linetest = ftest.readlines()
cnt = 0
while cnt < len(lineres):
  line = lineres[cnt].strip()
  linerep = linetest[cnt * 3].strip()  
  pairs = linerep.split(',')
  j = 0
  while j < len(pairs):
    if len(pairs[j].strip()) == 0:
      j += 1
      continue
    slot = pairs[j].split('=')[0].strip().lower()
    value = pairs[j].split('=')[1]
    value = value.replace('\"', '')
    line = line.replace(slot, value) 
    j += 1
  line = line[0].upper() + line[1:]
  ffinal.write(line + '\n')
  cnt += 1
ffinal.close() 
fres.close()
ftest.close()
