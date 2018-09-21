import random
f = open('stock_All.txt')
lines = f.readlines()
cnt = 0
lis = []
while cnt < len(lines):
  tmp = []
  l1 = lines[cnt].strip()
  l2 = lines[cnt + 1].strip()
  l3 = lines[cnt + 2].strip()
  tmp.append(l1)
  tmp.append(l2)
  tmp.append(l3)
  lis.append(tmp)
  cnt += 3
random.shuffle(lis)
cnt = 0
fw = open('stock_valid.txt', 'w')
fw2 = open('stock_test.txt', 'w')
for e in lis:
  if cnt < 250:
    print e[0]
    print e[1]
    print e[2]
  elif cnt < 280:
    fw.write(e[0].strip() + '\n')
    fw.write(e[1].strip() + '\n')
    fw.write(e[2].strip() + '\n')
  else:
    fw2.write(e[0].strip() + '\n')
    fw2.write(e[1].strip() + '\n')
    fw2.write(e[2].strip() + '\n')
  cnt += 1
f.close()
fw.close()
fw2.close()
