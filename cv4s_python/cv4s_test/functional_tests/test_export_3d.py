import pytest

from cv4s.app.endpoints import EXPORT_ENDPOINT
from cv4s_test.functional_tests.main_test_client import client
from cv4s_test.functional_tests.samples import test_operation_context


@pytest.mark.skipif(not test_operation_context, reason="No sample to test with")
def test_export_endpoint_success():
    """Check for valueError since"""
    response = client.post(url=EXPORT_ENDPOINT, json=test_operation_context)
    assert response.status_code == 200
