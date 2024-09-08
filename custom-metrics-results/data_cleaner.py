# source: https://stackoverflow.com/a/37307324

import pandas as pd

with open('./ADDED-commas-agents-deadlocked-results.json', encoding='utf-8') as inputfile:
    df = pd.read_json(inputfile)

df.to_csv('agents-deadlocked-results.csv', encoding='utf-8', index=False)