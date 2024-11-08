from executor import Executor
import threading
import time
import logging
import sys
import io

running = True

def funcao_repetitiva(intervalo, executor):
    while running:
        output = ""
        output = executor.getvalue()
        time.sleep(intervalo)
        with open("output.txt", "w") as f:
            f.write(output)

while True:
   response = input("Deseja iniciar o programa?\n")
   if response.lower() == "yes":
        running = True
        initialize = Executor('RQCT601N17W')
        initialize.new_request()
        initialize.app_instance = ["KidsMagicVoice"]
        t1 = threading.Thread(target=initialize.initialSetup)
        t2 = threading.Thread(target=funcao_repetitiva, args=(1,initialize))
        t2 = threading.Thread(target=funcao_repetitiva, args=(1,initialize.log_stream))
        t1.start()
        t2.start()
        t1.join()
        running = False
        initialize.cancellation_request()
        t2.join()
   else:
       break
