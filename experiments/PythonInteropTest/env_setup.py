import sys
import subprocess
from pathlib import Path

def create_env(venv_path, package_list_path):
    # create venv
    subprocess.run(['python', '-m', 'venv', venv_path])

    # install pip packages
    subprocess.run(f'{ Path(venv_path).joinpath("Scripts").joinpath("python") } -m pip install --upgrade -r { Path(package_list_path).joinpath("requirements.txt") }')

if __name__ == '__main__':
    create_env('testenv', 'PythonInteropTest/Plugins/add_operation')