# PayrocL4LB
Layer 4 Load balancer

How to use: Entry point is in project LoadBalancer.Demo -> Program.cs

Description

Implementation of Layer4 Load balancer as it would be 1999

Features "1999"

    Lack of async/await
    Implementation of EAP approach when developing load ballancer
    Not using lambda methods or modern syntactic sugar

When is solution NOT 1999

    Language used was published in 2000
    DummyService
    Moq as framework for unit tests

# Design overview:

Domain project:

    1. Defines contracts
    2. Defines model
    3. Define interfaces

Configuration project:
    Implements simple configuration allowing to start and use load balancer for:
    
    1. Different set of nodes
    2. Differnt Load balancing strategy
    3. Port on which LB is running
    4. Frequency of health check

Core project:
    Implementation of:
    
    1. Health check monitor dynamically maintaining list of healthy nodes
    2. Strategies for load balancer
    3. Event-based asynchronous pattern version of load balancer (async/await is forbidden due to 1999 requirement)

DummyService:

Simple implementation of Tcp listener to demonstrate and test load balancer

Demo project:

Simple demo, starts 3 dummy services, calls LB 10 times.
