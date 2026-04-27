grammar Mppg;

// lexer rules
NEW_LINE : [\r\n]+;
WHITE_SPACE : [ \t]+ -> skip;

NUMBER_ABS_LITERAL : INTEGER_LITERAL | DECIMAL_NUMBER_ABS_LITERAL | INFINITE_NUMBER_ABS_LITERAL;
INTEGER_LITERAL : [0-9]+;
DECIMAL_NUMBER_ABS_LITERAL : [0-9]+('.'[0-9]+)?;
INFINITE_NUMBER_ABS_LITERAL : 'inf'|'infinity'|'Infinity';

ASSIGN : ':=';
PLUS : '+';
MINUS : '-';
WEDGE : '/\\';
VEE : '\\/';
PROD_SIGN: '*';
DIV_SIGN: '/';
DIV_OP: 'div';
STRING_LITERAL : '"' ~([\r\n"])*? '"';
INLINABLE_COMMENT: ('//'|'%'|'#') [\p{L}\p{Nd}\p{P}\p{S} \t]*;
VARIABLE_NAME : [a-zA-Z_][a-zA-Z_0-9]*;

// parser rules
program : statementLine (NEW_LINE statementLine)* NEW_LINE? EOF;
statementLine: statement inlineComment? ;
statement 
    : assignment 
    | expressionCommand
    | plotCommand 
    | assertion
    | printExpressionCommand
    | comment
    | empty;
assignment : VARIABLE_NAME ASSIGN expression ;
expressionCommand : expression;
expression : functionExpression | numberExpression;
comment
    : INLINABLE_COMMENT
    // less precise that INLINABLE_COMMENT, but could not figure out a better way
    | '>' (~NEW_LINE)*?;
inlineComment: INLINABLE_COMMENT;
empty: ;

// Functions
functionExpression
    :  '(' functionExpression ')' #functionBrackets
    | PLUS functionExpression #functionPositive
    | MINUS functionExpression #functionNegative
    | functionExpression ('*'|'*_') functionExpression #functionMinPlusConvolution
    | functionExpression '*^' functionExpression #functionMaxPlusConvolution
    | functionExpression ('/'|'/_') functionExpression #functionMinPlusDeconvolution
    | functionExpression '/^' functionExpression #functionMaxPlusDeconvolution
    | functionExpression '*' numberEnclosedExpression #functionScalarMultiplicationLeft
    | numberEnclosedExpression '*' functionExpression #functionScalarMultiplicationRight
    | functionExpression '/' numberEnclosedExpression #functionScalarDivision
    | functionExpression op=(PLUS|MINUS|WEDGE|VEE) functionExpression #functionSumSubMinMax
    | functionExpression op=(PLUS|MINUS|WEDGE|VEE) numberEnclosedExpression #functionSumSubMinMax
    | functionExpression 'comp' functionExpression #functionComposition
    | 'star' '(' functionExpression ')' #functionSubadditiveClosure
    | ('hShift'|'hshift') '(' functionExpression ',' numberExpression ')' #functionHShift
    | ('vShift'|'vshift') '(' functionExpression ',' numberExpression ')' #functionVShift
    | ('inv'|'low_inv') '(' functionExpression ')' #functionLowerPseudoInverse
    | 'up_inv' '(' functionExpression ')' #functionUpperPseudoInverse
    | 'upclosure' '(' functionExpression ')' #functionUpNonDecreasingClosure
    | 'nnupclosure' '(' functionExpression ')' #functionNonNegativeUpNonDecreasingClosure
    | 'left-ext' '(' functionExpression ')' #functionLeftExt
    | 'right-ext' '(' functionExpression ')' #functionRightExt
    | functionConstructor #functionConstructorExp
    | VARIABLE_NAME #functionVariableExp
    ;

// Function constructors
functionConstructor 
    : rateLatency 
    | tokenBucket
    | affineFunction
    | stepFunction
    | stairFunction
    | delayFunction
    | zeroFunction
    | epsilonFunction
    | ultimatelyPseudoPeriodicFunction
    | ultimatelyAffineFunction
    ;

rateLatency : 'ratency' '(' numberExpression ',' numberExpression ')';
tokenBucket : 'bucket' '(' numberExpression ',' numberExpression ')';
affineFunction : 'affine' '(' numberExpression ',' numberExpression ')';
stepFunction : 'step' '(' numberExpression ',' numberExpression ')';
stairFunction : 'stair' '(' numberExpression ',' numberExpression ',' numberExpression ')';
delayFunction : 'delay' '(' numberExpression ')';
zeroFunction : 'zero' ;
epsilonFunction : 'epsilon' ;

// Ultimately Affine
ultimatelyAffineFunction: 'uaf' '(' sequence ')';

// Ultimately Pseudo-Periodic
ultimatelyPseudoPeriodicFunction: 'upp' '(' uppTransientPart?  uppPeriodicPart increment? ')';
uppTransientPart: sequence ',';
uppPeriodicPart: 'period' '(' sequence ')';
increment: ',' numberLiteral periodLenght?;
periodLenght: ',' numberLiteral;

// Segments
sequence: element+;
element: point | segment;
point: '[' endpoint ']';
segment
    : segmentLeftOpenRightOpen
    | segmentLeftOpenRightClosed
    | segmentLeftClosedRightOpen
    | segmentLeftClosedRightClosed
    ;
endpoint: '(' numberLiteral ',' numberLiteral ')';
segmentLeftOpenRightOpen: ']' endpoint numberLiteral? endpoint '[';
segmentLeftOpenRightClosed: ']' endpoint numberLiteral? endpoint ']';
segmentLeftClosedRightOpen: '[' endpoint numberLiteral? endpoint '[';
segmentLeftClosedRightClosed: '[' endpoint numberLiteral? endpoint ']';

// Numbers
numberExpression 
    : numberReturningfunctionOperation #numberReturningfunctionOperationExp 
    | '(' numberExpression ')' #numberBrackets
    | PLUS numberExpression #numberPositive
    | MINUS numberExpression #numberNegative
    | numberExpression op=(PROD_SIGN|DIV_SIGN|DIV_OP) numberExpression #numberMulDiv
    | numberExpression op=(PLUS|MINUS|WEDGE|VEE) numberExpression #numberSumSubMinMax
    | VARIABLE_NAME #numberVariableExp
    | numberLiteral #numberLiteralExp
    ;

numberEnclosedExpression
    : numberReturningfunctionOperation #encNumberReturningfunctionOperationExp
    | '(' numberExpression ')' #encNumberBrackets
    | VARIABLE_NAME #encNumberVariableExp
    | numberLiteral #encNumberLiteralExp
    ;

numberLiteral: (PLUS|MINUS)? NUMBER_ABS_LITERAL;

// Number-returning function operations
numberReturningfunctionOperation 
    : functionValueAt
    | functionLeftLimitAt
    | functionRightLimitAt
    | functionHorizontalDeviation 
    | functionVerticalDeviation;
functionValueAt: functionName '(' numberExpression ')';
functionLeftLimitAt: functionName '(' numberExpression '~'? MINUS ')';
functionRightLimitAt: functionName '(' numberExpression '~'? PLUS ')';
functionHorizontalDeviation : ('hDev'|'hdev') '(' functionExpression ',' functionExpression ')';
functionVerticalDeviation : ('vDev'|'vdev') '(' functionExpression ',' functionExpression ')';

// Plots
plotCommand: 'plot' '(' plotArg (',' plotArg)* ')';
plotArg: functionName | plotOption;
functionName: VARIABLE_NAME;
plotOption
    : 'main' '=' string
    | 'title' '=' string
    | 'xlim' '=' interval
    | 'ylim' '=' interval
    | 'xlab' '=' string
    | 'ylab' '=' string
    | 'out' '=' string
    | 'grid' '=' ('"no"'|'"yes"')
    | 'bg' '=' ('"no"'|'"yes"')
    | 'gui' '=' ('"no"'|'"yes"')
    ;

string
    : string '+' string
    | stringLiteral
    | stringVariable
    | numberLiteral;
stringLiteral: STRING_LITERAL;
stringVariable: VARIABLE_NAME;

interval: '[' numberLiteral ',' numberLiteral ']';

// Assertions
assertion
    : 'assert' '(' expression assertionOperator expression ')' ;
assertionOperator
    : '='
    | '!='
    | '<'   // custom addition
    | '<='
    | '>'   // custom addition
    | '>='
    ;

// extra commands
printExpressionCommand
    : 'printExpression' '(' VARIABLE_NAME ')';
