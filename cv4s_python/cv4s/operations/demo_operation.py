import cv2

from cv4s.data_types import BlobImages, OperationContext, RawImages
from cv4s.operations.export_to_3d import export_to_stl
from cv4s.operations.operation_base import OperationABC

WHITE = (255, 255, 255)


class DemoOperation(OperationABC):
    """
    Demo operation to showcase functionality of python operations.
    """

    center_x = 100
    center_y = 100
    radius = 50

    def run_operation(self, operation_context: OperationContext):
        self.draw_circles_all_layers(operation_context=operation_context)

    def draw_circles_all_layers(self, operation_context: OperationContext):
        blob_images = BlobImages.from_operation_context(operation_context)
        raw_images = RawImages.from_operation_context(operation_context)
        for i in range(raw_images.layer_count):
            img = raw_images.layers[i]
            cv2.circle(img, (self.center_x, self.center_y), self.radius, WHITE, -1)
            cv2.imshow("image", img)
            cv2.waitKey(0)
            cv2.destroyAllWindows()
