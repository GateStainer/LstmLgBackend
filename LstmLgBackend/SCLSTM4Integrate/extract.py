f = open('result.txt')
lines = f.readlines()
cnt = 0
while cnt < len(lines):
  top1 = lines[cnt ].strip()
  print top1
  cnt += 3
