eps = 1e-7

class BeamSearchNode(object):

    def __init__(self, h, c, sv, prevNode, wordvec, wordid, score, leng):
        self.h      = h
        self.c      = c
        self.sv      = sv
        self.score   = score
        self.leng   = leng
        self.wordvec = wordvec
        self.wordid = wordid
        self.prevNode = prevNode
    
    def eval(self):
        if self.leng > 40:
            return self.score / float(self.leng - 1 + eps) - 40.0
        return self.score / float(self.leng - 1 + eps)
