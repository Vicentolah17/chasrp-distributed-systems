# Distributed Systems Exercises – C# Projects

This repository contains a collection of **C# client/server applications** developed for a Distributed Systems course.  
Each exercise explores different aspects of networking, multithreading, and inter-process communication using **.NET**.

## Overview

The repository is divided into folders named `ex1`, `ex2`, `ex3`, etc.  
Each folder contains a standalone project implemented with `dotnet run`, following the structure of a client and a multithreaded server.

### Exercise 1 – Client/Server: Number Comparison
A simple client/server application where the client sends three numbers to the server, which returns the largest and smallest values.  
If the first number sent is negative, the server shuts down.

### Exercise 2 – Quadrilateral Object Exchange
A client creates and sends a `Quadrilateral` object with four sides to the server.  
The server determines the type (square, rectangle, or generic quadrilateral) and sends the updated object back to the client.

### Exercise 3 – Inventory Control System
A multithreaded server manages a small inventory based on client requests.  
Clients can add or remove product quantities.  
The server validates entries, prevents negative stock, and handles unknown products.

### Exercise 4 – Simple FTP
A client and server implementation that supports file transfer commands.  
The client can choose local and remote directories, upload, download, and execute basic remote commands.

### Exercise 5 – Multithreaded Web Crawler
A web crawler that reads an initial list of URLs and a list of search words from two files.  
It processes each page concurrently using a configurable number of threads, tracking word frequencies and discovered URLs.

## Technologies Used

- **C# / .NET 9**
- **Sockets and TCP/IP**
- **Multithreading**
- **Serialization**
- **File I/O**
- **Basic Web Scraping**

## About

These projects were developed as part of a Distributed Systems coursework.  
They serve as a study of how concurrent and networked applications operate in C#, focusing on clean implementation and practical design.
