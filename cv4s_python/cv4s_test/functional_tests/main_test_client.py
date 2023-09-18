from fastapi import FastAPI
from starlette.testclient import TestClient

from cv4s.app.operations_router import operations_router

app = FastAPI()
app.include_router(operations_router)
client = TestClient(app)
