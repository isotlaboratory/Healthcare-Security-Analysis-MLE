import numpy as np
import pandas as pd
import os


"""
This script uses the true weights/coefficients and features from the logistic regression classifer and its trainign/test set, multiplies 
them by varying powers of 10 (i.e. percisions), and uses these scaled weights/coefficients and features, converted 
to integers, to preform the weighted summation used in classification. The results are compared to determine the 
accuracy each percision achieves, and the weighted sums are ranked to see how much they agree with the ranks of the 
weighted sums found with the true/full-percision weights/coefficients and features
"""

coefs = pd.read_csv("ceofs_true.csv",header=None).to_numpy() #load coefficients from clf (includes bias)
X_raw =  pd.read_csv("feature_table.csv",header=None).to_numpy() #load unencrypted feature table
Y_true = pd.read_csv("Y.csv", header=None).to_numpy() #load labels


#use full precision features, weights, and bias
X_plain = np.append(np.ones((X_raw.shape[0],1)), X_raw, axis=1)
sums_plain = np.matmul(X_plain, coefs.T) #calculated weighted sums for unencrypted feature table

ranks_plain = sums_plain.argsort(axis=1).argsort(axis=1)#get ranks of sums for full precision sums

#count correct predictions
n_correct = 0
Y_enc_plain = np.argmax(sums_plain, axis=1)
for i in range(0,Y_enc_plain.shape[0]):
    if Y_enc_plain[i] == Y_true[i]:
        n_correct += 1

print("\nFull Precision:\t"+str(100*n_correct/Y_enc_plain.shape[0])+"% accuracy\n")

print("Precision\tPercent of Agreeing Ranks\tAccuracy")
for p in range(1,10): #for each power p

    precision = 10**p #get precision

    #convert fetures, bias, and coef to correct percision
    X_enc = (X_raw*precision).astype(int) #scale features
    X_enc =  np.append(np.ones((X_enc.shape[0],1))*precision, X_enc, axis=1) #append scaled bias
    coefs_cur = (coefs*precision).astype(int) #scale coefs
    
    #calculated weighted sums for scaled integer feature table
    sums_enc = np.matmul(X_enc, coefs_cur.T)
    
    Y_enc_cur = np.argmax(sums_enc, axis=1) #get predictions from sclaed integer sums

    ranks_enc = sums_enc.argsort(axis=1).argsort(axis=1) #get ranks of sums for scaled integer sums
    
    #count number of ranks which agree
    n_ranks_correct = 0
    for i in range(0,sums_plain.shape[0]):
        for j in range(22):
            if ranks_plain[i,j] == ranks_enc[i,j]:
                n_ranks_correct+=1

    #count correct predictions on 
    n_pred_correct = 0
    for i in range(0,Y_enc_cur.shape[0]):
        if Y_enc_cur[i] == Y_true[i]:
            n_pred_correct += 1

    print("10^"+str(p)+":\t\t"+"{:10.8f}".format(100*n_ranks_correct/(sums_plain.shape[0]*22))+"%\t\t\t"+"{:10.8f}".format(100*n_pred_correct/Y_enc_cur.shape[0])+"%")

