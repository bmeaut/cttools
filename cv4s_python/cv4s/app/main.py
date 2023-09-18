from fastapi import FastAPI

from cv4s.app.operations_router import operations_router

app = FastAPI()
app.include_router(operations_router)
