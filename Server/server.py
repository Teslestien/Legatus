from flask import Flask, request
from replit import db
from datetime import datetime
import json

app = Flask("Legatus", template_folder='templates', static_folder='static')


@app.route('/', methods=['GET'])
def start():
  return "hello"


@app.route('/receive', methods=['GET'])
def receive():
  receiver = request.args.get("user")
  to_receive = []
  messages = json.loads(open("messages.json").read())
  for i in range(len(messages)):
    print(i)
    if messages[i]["Sender"] != receiver:
      messages[i]["Content"] = messages[i]["Content"].replace(" ", "+")
      to_receive.append(messages[i])
      del messages[i]

  with open("messages.json", "w") as outfile:
    outfile.write(json.dumps(messages))
  messages = []
  return to_receive


@app.route('/send', methods=['GET'])
def send():
  messages = json.loads(open("messages.json").read())
  sender = request.args.get("user")
  message = request.args.get("message")
  time = datetime.now().strftime("%H:%M:%S:%f")
  msg = {"Sender": sender, "Time": time, "Content": message}
  print(msg)
  messages.append(msg)
  with open("messages.json", "w") as outfile:
    outfile.write(json.dumps(messages))
  print(sender, message, time, sep="\n")
  return "[]\n"


app.run(host="0.0.0.0", port=890)
