�L
YC:\Users\hw72786\Source\Repos\OverWatcher\OverWatcher\OverWatcher.Common.Logger\Logger.cs
	namespace 	
OverWatcher
 
. 
Common 
. 
Logging $
{ 
public 

class 
Logger 
{		 
private

 
static

 

Dictionary

 !
<

! "
Type

" &
,

& '
ILog

( ,
>

, -
	LoggerMap

. 7
;

7 8
static 
Logger 
( 
) 
{
	LoggerMap 
= 
new 

Dictionary &
<& '
Type' +
,+ ,
ILog- 1
>1 2
(2 3
)3 4
;4 5
} 	
private 
static 
ILog 
	GetLogger %
(% &
)& '
{ 	
var 

stackTrace 
= 
new  

StackTrace! +
(+ ,
), -
;- .
var 

methodBase 
= 

stackTrace '
.' (
GetFrame( 0
(0 1
$num1 2
)2 3
.3 4
	GetMethod4 =
(= >
)> ?
;? @
var 
type 
= 

methodBase !
.! "

;/ 0
if 
( 
	LoggerMap 
. 
ContainsKey %
(% &
type& *
)* +
)+ ,
{ 
return 
	LoggerMap  
[  !
type! %
]% &
;& '
} 
ILog 
logger 
= 

LogManager $
.$ %
	GetLogger% .
(. /
type/ 3
)3 4
;4 5
	LoggerMap 
. 
Add 
( 
type 
, 
logger  &
)& '
;' (
return 
logger 
; 
} 	
public 
static 
void 
Info 
(  
string  &
formattedString' 6
,6 7
params8 >
object? E
[E F
]F G
paramH M
)M N
{   	
	GetLogger!! 
(!! 
)!! 
.!! 
Info!! 
(!! 
string!! #
.!!# $
Format!!$ *
(!!* +
formattedString!!+ :
,!!: ;
param!!< A
)!!A B
)!!B C
;!!C D
}"" 	
public$$ 
static$$ 
void$$ 
Warn$$ 
($$  
string$$  &
formattedString$$' 6
,$$6 7
params$$8 >
object$$? E
[$$E F
]$$F G
param$$H M
)$$M N
{%% 	
	GetLogger&& 
(&& 
)&& 
.&& 
Warn&& 
(&& 
string&& #
.&&# $
Format&&$ *
(&&* +
formattedString&&+ :
,&&: ;
param&&< A
)&&A B
)&&B C
;&&C D
}'' 	
public)) 
static)) 
void)) 
Error))  
())  !
string))! '
formattedString))( 7
,))7 8
params))9 ?
object))@ F
[))F G
]))G H
param))I N
)))N O
{** 	
	GetLogger++ 
(++ 
)++ 
.++ 
Error++ 
(++ 
string++ $
.++$ %
Format++% +
(+++ ,
formattedString++, ;
,++; <
param++= B
)++B C
)++C D
;++D E
},, 	
public.. 
static.. 
void.. 
Fatal..  
(..  !
string..! '
formattedString..( 7
,..7 8
params..9 ?
object..@ F
[..F G
]..G H
param..I N
)..N O
{// 	
	GetLogger00 
(00 
)00 
.00 
Fatal00 
(00 
string00 $
.00$ %
Format00% +
(00+ ,
formattedString00, ;
,00; <
param00= B
)00B C
)00C D
;00D E
}11 	
public44 
static44 
void44 
Warn44 
(44  
	Exception44  )
ex44* ,
,44, -
string44. 4
formattedString445 D
,44D E
params44F L
object44M S
[44S T
]44T U
param44V [
)44[ \
{55 	
	GetLogger66 
(66 
)66 
.66 
Warn66 
(66 
string66 #
.66# $
Format66$ *
(66* +
formattedString66+ :
,66: ;
param66< A
)66A B
,66B C
ex66D F
)66F G
;66G H
}77 	
public99 
static99 
void99 
Error99  
(99  !
	Exception99! *
ex99+ -
,99- .
string99/ 5
formattedString996 E
,99E F
params99G M
object99N T
[99T U
]99U V
param99W \
)99\ ]
{:: 	
	GetLogger;; 
(;; 
);; 
.;; 
Error;; 
(;; 
string;; $
.;;$ %
Format;;% +
(;;+ ,
formattedString;;, ;
,;;; <
param;;= B
);;B C
,;;C D
ex;;E G
);;G H
;;;H I
}<< 	
public>> 
static>> 
void>> 
Fatal>>  
(>>  !
	Exception>>! *
ex>>+ -
,>>- .
string>>/ 5
formattedString>>6 E
,>>E F
params>>G M
object>>N T
[>>T U
]>>U V
param>>W \
)>>\ ]
{?? 	
	GetLogger@@ 
(@@ 
)@@ 
.@@ 
Fatal@@ 
(@@ 
string@@ $
.@@$ %
Format@@% +
(@@+ ,
formattedString@@, ;
,@@; <
param@@= B
)@@B C
,@@C D
ex@@E G
)@@G H
;@@H I
}AA 	
publicDD 
staticDD 
voidDD 
DebugDD  
(DD  !
stringDD! '
formattedStringDD( 7
,DD7 8
paramsDD9 ?
objectDD@ F
[DDF G
]DDG H
paramDDI N
)DDN O
{EE 	
	GetLoggerFF 
(FF 
)FF 
.FF 
DebugFF 
(FF 
stringFF $
.FF$ %
FormatFF% +
(FF+ ,
formattedStringFF, ;
,FF; <
paramFF= B
)FFB C
)FFC D
;FFD E
}GG 	
publicII 
staticII 
voidII 
InfoII 
(II  
stringII  &
logII' *
)II* +
{JJ 	
	GetLoggerKK 
(KK 
)KK 
.KK 
InfoKK 
(KK 
logKK  
)KK  !
;KK! "
}LL 	
publicNN 
staticNN 
voidNN 
WarnNN 
(NN  
	ExceptionNN  )
exNN* ,
,NN, -
stringNN. 4
logNN5 8
)NN8 9
{OO 	
	GetLoggerPP 
(PP 
)PP 
.PP 
WarnPP 
(PP 
logPP  
,PP  !
exPP" $
)PP$ %
;PP% &
}QQ 	
publicSS 
staticSS 
voidSS 
ErrorSS  
(SS  !
	ExceptionSS! *
exSS+ -
,SS- .
stringSS/ 5
logSS6 9
)SS9 :
{TT 	
	GetLoggerUU 
(UU 
)UU 
.UU 
ErrorUU 
(UU 
logUU !
,UU! "
exUU# %
)UU% &
;UU& '
}VV 	
publicXX 
staticXX 
voidXX 
FatalXX  
(XX  !
	ExceptionXX! *
exXX+ -
,XX- .
stringXX/ 5
logXX6 9
)XX9 :
{YY 	
	GetLoggerZZ 
(ZZ 
)ZZ 
.ZZ 
FatalZZ 
(ZZ 
logZZ !
,ZZ! "
exZZ# %
)ZZ% &
;ZZ& '
}[[ 	
public]] 
static]] 
void]] 
Warn]] 
(]]  
string]]  &
log]]' *
)]]* +
{^^ 	
	GetLogger__ 
(__ 
)__ 
.__ 
Warn__ 
(__ 
log__  
)__  !
;__! "
}`` 	
publicbb 
staticbb 
voidbb 
Errorbb  
(bb  !
stringbb! '
logbb( +
)bb+ ,
{cc 	
	GetLoggerdd 
(dd 
)dd 
.dd 
Errordd 
(dd 
logdd !
)dd! "
;dd" #
}ee 	
publicgg 
staticgg 
voidgg 
Fatalgg  
(gg  !
stringgg! '
loggg( +
)gg+ ,
{hh 	
	GetLoggerii 
(ii 
)ii 
.ii 
Fatalii 
(ii 
logii !
)ii! "
;ii" #
}jj 	
publicll 
staticll 
voidll 
Debugll  
(ll  !
stringll! '
logll( +
)ll+ ,
{mm 	
	GetLoggernn 
(nn 
)nn 
.nn 
Debugnn 
(nn 
lognn !
)nn! "
;nn" #
}oo 	
}pp 
}qq �
jC:\Users\hw72786\Source\Repos\OverWatcher\OverWatcher\OverWatcher.Common.Logger\Properties\AssemblyInfo.cs
[ 
assembly 	
:	 


( 
$str 4
)4 5
]5 6
[		 
assembly		 	
:			 

AssemblyDescription		 
(		 
$str		 !
)		! "
]		" #
[

 
assembly

 	
:

	 
!
AssemblyConfiguration

  
(

  !
$str

! #
)

# $
]

$ %
[ 
assembly 	
:	 

AssemblyCompany 
( 
$str 
) 
] 
[ 
assembly 	
:	 

AssemblyProduct 
( 
$str 6
)6 7
]7 8
[
assembly
:

AssemblyCopyright
(
$str
)
]
[ 
assembly 	
:	 

AssemblyTrademark 
( 
$str 
)  
]  !
[ 
assembly 	
:	 

AssemblyCulture 
( 
$str 
) 
] 
[ 
assembly 	
:	 

log4net 
. 
Config 
. 
XmlConfigurator )
() *

ConfigFile* 4
=5 6
$str7 G
,G H
ConfigFileExtension 
= 
$str 
,  
Watch! &
=' (
false) .
). /
]/ 0
[ 
assembly 	
:	 


ComVisible 
( 
false 
) 
] 
[ 
assembly 	
:	 

Guid 
( 
$str 6
)6 7
]7 8
[$$ 
assembly$$ 	
:$$	 

AssemblyVersion$$ 
($$ 
$str$$ $
)$$$ %
]$$% &
[%% 
assembly%% 	
:%%	 

AssemblyFileVersion%% 
(%% 
$str%% (
)%%( )
]%%) *