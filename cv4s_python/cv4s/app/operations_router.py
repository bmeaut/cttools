from fastapi import APIRouter

from cv4s.app.endpoints import (
    CREATE_TEST_CONTEXT_ENDPOINT,
    DEMO_OPERATION_RUN_ENDPOINT,
    EXPORT_ENDPOINT,
)
from cv4s.data_types import OperationContext
from cv4s.operations.demo_operation import DemoOperation
from cv4s.operations.export_to_3d import Export3DOperation
from cv4s.operations.save_context import SaveOperation

operations_router = APIRouter()

DEMO_OPERATION = DemoOperation()
SAVE_OPERATION = SaveOperation()
EXPORT_OPERATION = Export3DOperation()


@operations_router.post(DEMO_OPERATION_RUN_ENDPOINT)
def run_demo_operation(operation_context: OperationContext):
    result = DEMO_OPERATION.run_operation(operation_context=operation_context)
    return result


@operations_router.post(CREATE_TEST_CONTEXT_ENDPOINT)
def create_context(operation_context: OperationContext):
    return SAVE_OPERATION.run_operation(operation_context=operation_context)


@operations_router.post(EXPORT_ENDPOINT)
def export_stl(operation_context: OperationContext):
    return EXPORT_OPERATION.run_operation(operation_context=operation_context)
