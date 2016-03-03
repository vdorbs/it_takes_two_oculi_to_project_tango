#!/usr/bin/python

# Alice's Python Program
# Repeatedly prompts the user for a string then sends that string to the given address
import socket
import sys

TCP_IP = "localhost"
TCP_PORT = 7569

BUFFER_SIZE = 1024

# attempt to connect to Mallory/Bob
try:
  s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
  s.connect((TCP_IP, TCP_PORT))
except Exception as e:
  print "Connection failed"
  exit(0)


while 1:
  msg = "" + raw_input("Enter a message you'd like to send: \n")
  s.send(msg)

s.close()

