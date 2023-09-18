from typing import Dict, List

import numpy as np
from fastapi_camelcase import CamelModel
from pydantic.main import BaseModel

Matrix = List[List[List[int]]]


class Tag(CamelModel):
    Name: str
    Value: int


class OperationContext(BaseModel):
    BlobImages: Matrix  # 3D matrix holding blobimages
    RawImages: Matrix  # 3D matrix holding raw images
    Tags: Dict[int, List[Tag]]


class SampleScan:
    """Holds and converts data for scans represented in a 3D matrix"""

    def __init__(self, layer_count: int = 1, width: int = 512, height: int = 512):
        self.layers = np.zeros((layer_count, width, height), dtype="uint8")
        self.layer_count = layer_count

    @classmethod
    def from_matrix(cls, matrix: Matrix):
        layer_count = len(matrix)
        layers = None
        if layer_count > 0:
            width = len(matrix[0])
            height = len(matrix[0][0])
            layers = np.zeros((layer_count, width, height), dtype="uint8")
            for i in range(layer_count):
                layers[i] = np.array(matrix[i])

        sample_scan = cls()
        sample_scan.layers = layers
        sample_scan.layer_count = layer_count
        return sample_scan


class BlobImages(SampleScan):
    @classmethod
    def from_operation_context(cls, operation_context: OperationContext):
        return cls.from_matrix(operation_context.BlobImages)


class RawImages(SampleScan):
    @classmethod
    def from_operation_context(cls, operation_context: OperationContext):
        return cls.from_matrix(operation_context.RawImages)
