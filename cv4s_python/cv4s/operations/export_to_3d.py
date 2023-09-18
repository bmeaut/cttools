import numpy as np
import trimesh
from skimage import measure

from cv4s.data_types import OperationContext, RawImages
from cv4s.operations.operation_base import OperationABC


def export_to_stl(layers: np.ndarray):
    verts, faces, normals, values = measure.marching_cubes(layers, 0)

    mesh = trimesh.Trimesh(vertices=verts, faces=faces)
    mesh.export("export.stl")
    print("Exported layers successfully")


class Export3DOperation(OperationABC):
    def run_operation(self, operation_context: OperationContext):
        raw_images = RawImages.from_operation_context(operation_context)
        self.generate_3d(layers=raw_images.layers)

    @staticmethod
    def generate_3d(layers: np.ndarray):
        export_to_stl(layers)
