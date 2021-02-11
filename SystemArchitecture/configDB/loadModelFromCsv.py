import pandas as pd
import numpy as np
from pymongo import MongoClient


mongo = MongoClient('localhost', 27017)

coefs =  pd.read_csv("coefs.csv", header=None).to_numpy()

M_classes = coefs.shape[0]
N_weights = coefs.shape[1]

Precision = 1000

modelType = "LogisticRegression"

aDict = {"MClasses":M_classes, "NWeights":N_weights,"Type":modelType, "Precision":Precision, "Weights":coefs.tolist()}

mongo.models["models"].insert_one(aDict)