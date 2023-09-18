import pytest

from cv4s.app.endpoints import DEMO_OPERATION_RUN_ENDPOINT
from cv4s_test.functional_tests.main_test_client import client
from cv4s_test.functional_tests.samples import (
    empty_sample,
    fix_intensity_sample,
    noise_sample,
)


@pytest.mark.parametrize("layer_count", [1, 2])
def test_demo_endpoint_success(layer_count):
    operation_context = {
        "BlobImages": empty_sample(layer_count),
        "RawImages": fix_intensity_sample(layer_count),
        "Tags": {},
    }
    response = client.post(url=DEMO_OPERATION_RUN_ENDPOINT, json=operation_context)

    assert response.status_code == 200


@pytest.mark.parametrize("layer_count", [1, 2])
def test_demo_endpoint_noise_success(layer_count):
    operation_context = {
        "BlobImages": empty_sample(layer_count),
        "RawImages": noise_sample(layer_count),
        "Tags": {},
    }
    response = client.post(url=DEMO_OPERATION_RUN_ENDPOINT, json=operation_context)

    assert response.status_code == 200
