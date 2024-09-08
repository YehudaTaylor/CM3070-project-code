# modified from: https://stackoverflow.com/a/37307324

import pandas as pd

results = ["./bt-ai-run-1.json"]

for result in results:
    with open(result, encoding='utf-8') as inputfile:
        df = pd.read_json(inputfile)

    df.to_csv(result + ".csv", encoding='utf-8', index=False)