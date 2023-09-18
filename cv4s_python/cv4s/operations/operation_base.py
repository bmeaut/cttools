from abc import ABCMeta

from cv4s.data_types import OperationContext


class OperationABC(metaclass=ABCMeta):
    def run_operation(self, operation_context: OperationContext):
        pass
