# Epoxid

Epoxid is an implementation of the [Python language](https://www.python.org/) on the [.NET platform](https://dotnet.microsoft.com/). Unlike [IronPython](https://ironpython.net/), my goal is to make *Python-first* compiler. It means that Epoxid wouldn't contain runtime that tries to include tools for all dynamic languages of the Iron-family and focus only on Python semantics specifically.

## Motivation of the project

First of all, it is a sort of the learning project cause i am new in the programming and language creating. But it doesn't mean that i won't develop it properly.
Also i consider Epoxid as a long-term project. At least i would try to support it as long as it gives me feedback would it be just green tests or something another. So if you want to support this project too, even small activity would be a big motivation for me :)

In the second i have goal to make good alternative for the [Python.NET](https://pythonnet.github.io/) library, IronPython and another projects that tries to make .NET friend for Python. It doesn't means that these projects are bad, but they are not native for the modern .NET with all of AOT and source generation stuff. And i want to try fix it.

## State of the project

### Here is the my current goals:
- [x] Run `print("Bau Bau!")`
- [x] Turing complete
- [ ] Basic classes, functions, simple object semantics
- [x] Bytecode VM (on top of the CLR)
- [ ] Calling CLR code
- [ ] Full Python syntax support
- [ ] Exporting code to CLR
- [ ] Pure (almost) Python support
- [ ] Std re-implementation (using .NET std as backend)
- [ ] Another cool stuff

### v0.2.0 roadmap:

- User-function declarations
- User-class declarations
- Support more syntax
- Object attributes
- Module imports
- `sys` module subset
- `for`-loops, iterator classes
- `list` class

## Getting started

**Requirements**: .NET10

#### Install repository locally:

```bash
git clone https://github.com/NiyazBiyaz/epoxid
```

#### Get latest changes:

```bash
git pull
```

#### Run sample script:

```bash
cd src/Epoxid/
dotnet run __hello__.ep
```

#### Run tests:

```bash
dotnet test
```

#### Regenerate parser:

```bash
# Make a change to src/Epoxid/SyntaxAnalysis/Python.ebnf before
./RegenerateParser.sh
```

#### Create executables:

```bash
./Publish.sh
```

### Code examples:

**[FizzBuzz](https://wikipedia.org/wiki/Fizz_buzz)** game in Epoxid with some extensions and adapted to the current state of project:

```epoxid
# See source in src/Epoxid/__fluffyFuzzy__.ep
phrase = input("Say something: ")
number = 0
while number < 31:
    if number % 3 == 0 and number % 5 == 0:
        print("Bau bau!")
    elif number % 3 == 0:
        print("Fluffy")
    elif number % 5 == 0:
        print("Fuzzy")
    else:
        print(number, phrase)
    number += 1
```

**Fibonacci number** computing function. Since 'int' type is a 32-bit integer, it cannot compute large numbers, also it would be unchecked.

```epoxid
# See source in src/Epoxid/__fibonacci__.ep
def fibonacci(number):
    first = 0 
    second = 1
    counter = 0
    while counter < number:
        tmp = first + second
        first = second
        second = tmp
        counter += 1
    return first

print(fibonacci(42))
```

---

## License

This project is licensed under the MIT License. See *LICENSE* file for full text.
