import json
from pathlib import Path
from random import randint

source_root = Path(__file__).resolve().parent.parent.parent
after_threshold_context_path = source_root.joinpath(
    "bin", "test_contexts", "after_threshold_context.json"
)
test_operation_context = None
if after_threshold_context_path.exists():
    with open(str(after_threshold_context_path), "r") as f:
        data = f.read()
        test_operation_context = json.loads(data)


def noise_sample(layer_count: int = 1):
    raw_images = [
        [[randint(0, 255) for i in range(512)] for j in range(512)]
        for k in range(layer_count)
    ]
    return raw_images


def fix_intensity_sample(layer_count: int = 1):
    intensity = 100
    raw_images = [
        [[intensity for i in range(512)] for j in range(512)]
        for k in range(layer_count)
    ]
    return raw_images


def empty_sample(layer_count: int = 1):
    return [[[]] for _ in range(layer_count)]
