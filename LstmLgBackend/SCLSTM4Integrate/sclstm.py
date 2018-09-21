'''
Building Semantically Conditioned LSTM block.
'''

from __future__ import division
import cntk
import numpy as np
from cntk.variables import Constant, Parameter
from cntk.ops import times, slice, sigmoid, tanh, softplus
from cntk.internal import _as_tuple
from cntk.initializer import glorot_uniform
from cntk.layers import Dense, Label
from _cntk_py import InferredDimension
from cntk.default_options import get_default_override, default_override_or
from cntk.ops.functions import Function
from cntk.ops.functions import BlockFunction 

_INFERRED = (InferredDimension,)  # as a tuple, makes life easier

def _RecurrentBlock(type, shape, cell_shape, activation, use_peepholes,
                    init, init_bias,
                    enable_self_stabilization,
                    name=''):

    has_projection = cell_shape is not None
    shape = _as_tuple(shape)

    cell_shape = _as_tuple(cell_shape) if cell_shape is not None else shape
    if len(shape) != 1 or len(cell_shape) != 1:
        raise ValueError("%s: shape and cell_shape must be vectors (rank-1 tensors)" % type)

    stack_axis = -1  
    # determine stacking dimensions
    cell_shape_list = list(cell_shape)
    cell_shape_list_W = list(cell_shape)
    cell_shape_list_H = list(cell_shape)
    cell_shape_stacked = tuple(cell_shape_list)  
    shape_list = list(shape)

    # slot value pair onehot vector dimension
    sv_dim = cell_shape_list[stack_axis]
    stacked_dim = shape_list[stack_axis]
    sv_shape_stacked = tuple([sv_dim])  

    # 3*hidden_dim
    cell_shape_list[stack_axis] = stacked_dim  * {
        'LSTM': 3
    }[type] 
    cell_shape_stacked = tuple(cell_shape_list)  

    # 2*hidden_dim + sv_dim
    cell_shape_list_H[stack_axis] = stacked_dim * {
        'LSTM': 2
    }[type] + sv_dim
    cell_shape_stacked_H = tuple(cell_shape_list_H)  

    cell_shape_list_W[stack_axis] = stacked_dim * {
        'LSTM': 2
    }[type] 
    cell_shape_stacked_W = tuple(cell_shape_list_W)  

    # parameters
    b  = Parameter(            cell_shape_stacked,   init=init_bias, name='b')                              # bias
    brg  = Parameter(            sv_shape_stacked,   init=init_bias, name='brg')                              # bias
    W  = Parameter(_INFERRED + cell_shape_stacked,   init=init,      name='W')
    Wrg  = Parameter(_INFERRED + sv_shape_stacked,   init=init,      name='Wrg')
    Wcx  = Parameter(_INFERRED + shape,   init=init,      name='Wcx') 

    H  = Parameter(shape     + cell_shape_stacked, init=init,      name='H')
    Hrg  = Parameter(shape     + sv_shape_stacked, init=init,      name='Hrg')
    Hcx  = Parameter(shape     + shape, init=init,      name='Hcx')  
    Hsv  = Parameter(sv_shape_stacked     + cell_shape_stacked, init=init,      name='Hsv')
    Hsvrg  = Parameter(sv_shape_stacked     + sv_shape_stacked, init=init,      name='Hsvrg') 
    Wfc  = Parameter(sv_shape_stacked     + shape, init=init,      name='Wfc')

    # LSTM model function
    # in this case:
    #   (dh, dc, sv, x) --> (h, c, sv)

    def lstm(dh, dc, sv, x):

        # projected contribution from input(s), hidden, and bias
        proj3 = b + times(x, W) + times(dh, H) + times(sv, Hsv)

        it_proj  = slice (proj3, stack_axis, 0*stacked_dim, 1*stacked_dim)  
        ft_proj  = slice (proj3, stack_axis, 1*stacked_dim, 2*stacked_dim)
        ot_proj  = slice (proj3, stack_axis, 2*stacked_dim, 3*stacked_dim)

        it = sigmoid (it_proj)        # input gate(t)
        ft = sigmoid (ft_proj)        # forget-me-not gate(t)
        ot = sigmoid (ot_proj)    # output gate(t)

        # the following is reading gate
        proj3rg = sigmoid(times(x, Wrg) + times(dh, Hrg) + times(sv, Hsvrg) + brg)
        v = proj3rg * sv

        cx_t = tanh(times(x, Wcx) + times(dh, Hcx))

        # need to do stablization ?? 
        # update memory cell 
        c = it * cx_t + ft * dc  + tanh(times(v, Wfc))

        h = ot * tanh(c)

        return (h, c, v)

    function = {
        'LSTM':    lstm
    }[type]

    # return the corresponding lambda as a CNTK Function
    return BlockFunction(type, name)(function)

def LSTM(shape, cell_shape=None, activation=default_override_or(tanh), use_peepholes=default_override_or(False),
         init=default_override_or(glorot_uniform()), init_bias=default_override_or(0),
         enable_self_stabilization=default_override_or(False),
         name=''):

    activation                = get_default_override(LSTM, activation=activation)
    use_peepholes             = get_default_override(LSTM, use_peepholes=use_peepholes)
    init                      = get_default_override(LSTM, init=init)
    init_bias                 = get_default_override(LSTM, init_bias=init_bias)
    enable_self_stabilization = get_default_override(LSTM, enable_self_stabilization=enable_self_stabilization)

    return _RecurrentBlock('LSTM', shape, cell_shape, activation=activation, use_peepholes=use_peepholes,
                           init=init, init_bias=init_bias,
                           enable_self_stabilization=enable_self_stabilization, name=name)

