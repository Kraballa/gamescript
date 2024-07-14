# GScript scripting language
'GScript' (gamescript) is an interpreted (for now) *statically* typed programming language. It was created using the parser generator Antlr. I may potentially use it in future games which is why it's called 'gamescript'. The main purpose of this project is the study of programming language design and development.

## Features
- 4 types: int, float, string, bool
- functions, both native (defined inside gamescript), and external (callback to csharp)
- scoped variables, name overloading and parameter overloading with a `global` keyword
- nested functions, function scoping and overloading
- statically typed variables

## Missing Features
- no array types
- typed function returns

## Code Examples
For a lot of code check `testscript.txt`, it contains the entire test suite with which the language is validated. Here are some smaller code examples.

### Scoping Behavior
```
function assert(bool ojb){
    if(!obj){
        print("error, assertion failed");
    }
}

int test = 0;
function testA(int test){
    assert(test == 1);
    assert(global.test == 0);

    function testB(int test){
        assert(test == 2);
        assert(global.test == 0);
    }
    testB(2);
}
testA(1);
```

### Random function
Dice roll example making use of the external `rand()` function:
```
int roll = (rand()*6)|int;
```

### Operator Precedence
Operator precedence and boolean logic work as expected.
```
[...]
assert(true);
assert(!false);
assert(true | false);
assert(!(true & false));
assert(true | false & false); //fails if 'and/or' precedence faulty

assert(1);
assert(-1+2);
assert(0.001);
```

### Power Function
```
[...]
function pow(float val, int power){
    float ret = 1;
    int i = 0;
    while(i < power){
        ret = ret * val;
        i = i+1;
    }
    return ret;
}

assert(pow(4,0) - 1 < 0.001);
assert(pow(4,1) - 4 < 0.001);
assert(pow(4,2) - 16 < 0.001);
assert(pow(4,3) - 64 < 0.001);
```