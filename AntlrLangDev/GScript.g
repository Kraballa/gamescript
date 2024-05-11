grammar GScript;

program: line+ EOF;

line: statement | ifBlock | whileBlock | block;

statement: (assignment | functionCall) ';';

ifBlock: 'if' '(' expression ')' block ('else' elseIfBlock)?;

elseIfBlock: block | ifBlock;

whileBlock: 'while' '(' expression ')' block;

block: '{' line* '}';

assignment: IDENTIFIER '=' expression;

functionCall: IDENTIFIER '(' (expression (',' expression)*)? ')';

expression
    : constant                              #constantExpression
    | IDENTIFIER                            #identifierExpression
    | functionCall                          #functionCallExpression
    | '(' expression ')'                    #enclosedExpression
    | '!' expression                        #negatedExpression
    | expression multOp expression          #multExpression
    | expression addOp expression           #addExpression
    | expression compareOp expression       #compareExpression
    | expression boolOp expression          #boolExpression
    ;

multOp: '*' | '/' | '%';
addOp: '+' | '-';
compareOp: '==' | '!=' | '>' | '<' | '>=' | '<=';
boolOp: BOOL_OP;

BOOL_OP: '&' | '|';

constant: INTEGER | FLOAT | STRING | BOOL | NULL;


// lexer

INTEGER: [1-9][0-9]*;
FLOAT: [0-9]* '.' [0-9]*;
STRING: ('"' ~'"'* '"') | ('\'' ~'\''* '\'');
BOOL: 'true' | 'false';
NULL: 'null';

IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;

WS: [\t\r\n]+ -> skip;