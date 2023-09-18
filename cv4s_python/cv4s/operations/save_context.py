from cv4s.data_types import OperationContext
from cv4s.operations.operation_base import OperationABC

FOLDER_NAME = "test_contexts"
FILE_NAME = "last_saved_context"


class SaveOperation(OperationABC):
    def run_operation(self, operation_context: OperationContext):
        context_to_save = operation_context.json()
        with open(f"{FOLDER_NAME}/{FILE_NAME}.json", "w") as f:
            f.write(context_to_save)
