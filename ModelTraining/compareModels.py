import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.svm import LinearSVC, SVC
from sklearn.model_selection import GridSearchCV, cross_val_score, cross_validate, StratifiedKFold
from sklearn.feature_selection import RFE
from sklearn.metrics import cohen_kappa_score, make_scorer, accuracy_score
from joblib import dump, load
from sklearn.preprocessing import MinMaxScaler, StandardScaler
from warnings import simplefilter
from sklearn.exceptions import ConvergenceWarning
from sklearn.feature_selection import chi2, mutual_info_classif
from copy import deepcopy
import matplotlib.pyplot as plt

"""
This script uses the average accuracy across 5-fold cross validation to compare the preformance of different models, with different parameter settings, feature subsets, and feature selction techniques.
"""

def notrans(X):
    return X

#ignore warnings
simplefilter(action='ignore', category=ConvergenceWarning)
simplefilter(action='ignore', category=FutureWarning)

#load data
X_all =  pd.read_csv("feature_table.csv", header=None).to_numpy() #load unencrypted feature table
Y = pd.read_csv("Y.csv", header=None).to_numpy().reshape((-1,))

#get sort indicies of mutual importance scores
disMask = [False, False] + ([True] * (X_all.shape[1] - 2 ) )  #tells mi_score which features are discrete
mi_scores = mutual_info_classif(X_all, Y, discrete_features=disMask )
mi_inds = np.argsort(mi_scores)

#get sort indicies of chi^2 scores 
chi_scores = chi2(X_all,Y)[0]
chi_inds = np.argsort(chi_scores)

#np.save("chi_inds.npy",chi_inds)
#np.save("mi_inds.npy",mi_inds)
#chi_inds = np.load("Data/chi_inds.npy")
#mi_inds = np.load("Data/mi_inds.npy")

#classifiers used and their names
classifiers = [LogisticRegression(), RandomForestClassifier(), LinearSVC(), SVC()]
strClf = ["Logistic Regression","Random Forest","Linear SVM","Non-Linear SVM"]

#Transformations used and their naemes
scaler1 = MinMaxScaler()
scaler2 = StandardScaler()
trans = [notrans, scaler1.fit_transform, scaler2.fit_transform, np.log, np.log1p]
strTrans = ["None","MinMax","Standardize","log()","log(x+1)"]

#parameter set for grid search for each classifier
parameterSets = [
    {"penalty":['l1','l2'], 'C':[0.1,1, 10], 'solver':["liblinear","lbfgs"]},
    {'criterion':['gini', 'entropy'], 'n_estimators':[100,200,300], 'bootstrap':[False, True]},
    {'penalty':['l1', 'l2'], 'C':[0.1, 1, 10], 'max_iter':[10000], 'loss':['hinge', 'squared_hinge']},
    {'kernel':['poly', 'rbf'], 'C':[0.1, 1, 10]}
]

#File scores are outputted to
fp = open("ModelComparison.txt","w")

max_score = 0.0 #keep track of best score
winnerString = "" #record best params and feature transformation/selection

for n_clf, clf in enumerate(classifiers): #for each classifier

    fp.write(""+str(strClf[n_clf])+":\n")

    for n_trans in range(0,5): #for each transformation

        fp.write("\t"+str(strTrans[n_trans])+":\n")

        #copy features before preform transformation
        X = deepcopy(X_all)

        #transform features
        X[1:3] = trans[n_trans](X[1:3] + np.finfo(float).eps)

        for n_feat in [2500,3500,4500]: #for each number of least informative features to remove

            fp.write("\t\tChi_feat: "+str(n_feat)+"\n")

            #remove unimportant features using chi
            X_selected = X[:,chi_inds[n_feat:]]

            #grab parameter set for grid search
            parameters = parameterSets[n_clf]

            #preform gridsearch
            GS = GridSearchCV(clf, parameters, n_jobs=-1)
            GS.fit(X_selected, Y)

            #write results (best params and score)
            fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
            for (x, y) in GS.best_params_.items():
                fp.write(str(x)+":"+str(y)+" ")
            fp.write("\n")

            if GS.best_score_ > max_score:
                winnerString = strClf[n_clf]+" is the winner with "+str(n_feat)+" features removed using Chi_squared, "+strTrans[n_trans]+" feature transformation, and the following params:"
                winnerString += str(GS.best_params_)
                max_score = GS.best_score_

            #-------------repeat above for MI
            fp.write("\t\tMI_feat: "+str(n_feat)+"\n")

            #remove unimportant features using chi
            X_selected = X[:,mi_inds[n_feat:]]

            #grab parameter set for grid search
            parameters = parameterSets[n_clf]

            #preform gridsearch
            GS = GridSearchCV(clf, parameters, n_jobs=-1)
            GS.fit(X_selected, Y)

            #write results (best params and score)
            fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
            for (x, y) in GS.best_params_.items():
                fp.write(str(x)+":"+str(y)+" ")
            fp.write("\n")

            if GS.best_score_ > max_score:
                winnerString = strClf[n_clf]+" is the winner with "+str(n_feat)+" features removed using Mutual Importance, "+strTrans[n_trans]+" feature transformation, and the following params:"
                winnerString += str(GS.best_params_)
                max_score = GS.best_score_

    fp.write("\n\n")


#repeat above using all features
fp.write("All features\n")
for n_clf, clf in enumerate(classifiers):
    
    fp.write("\t"+str(strClf[n_clf])+":\n")

    for n_trans in range(0,5):

        fp.write("\t\t"+str(strTrans[n_trans])+":\n")
        
        #copy features before preform transformation
        X = deepcopy(X_all)

        #transform features
        X[1:3] = trans[n_trans](X[1:3] + np.finfo(float).eps)
        
        #grab parameter set for grid search
        parameters = parameterSets[n_clf]

        #preform gridsearch
        GS = GridSearchCV(clf, parameters, n_jobs=-1)
        GS.fit(X, Y)

        #write results (best params and score)
        fp.write("\t\t\tAcc:"+"{:0.16f}".format(GS.best_score_)+"\t")
        for (x, y) in GS.best_params_.items():
            fp.write(str(x)+":"+str(y)+" ")
        fp.write("\n")

        if GS.best_score_ > max_score:
            winnerString = strClf[n_clf]+" is the winner using all features, "+strTrans[n_trans]+" feature transformation, and the following params:"
            winnerString += str(GS.best_params_)
            max_score = GS.best_score_

fp.write("\n\nBest Preforming Model:\n")
fp.write("Accuracy: "+str(max_score)+"\n")
fp.write(str(winnerString)+"\n")

fp.close()