import numpy as np

def run(input):
    print("AUTO_VENV")
    print(input.Text)
    return np.array([input.A, input.B]) - np.array([input.C, input.D]), "Test result text"
