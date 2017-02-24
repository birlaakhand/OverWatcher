º
iC:\Users\hw72786\Source\Repos\OverWatcher\OverWatcher\OverWatcher.Common.DateTimeHelper\DateTimeHelper.cs
	namespace 	
OverWatcher
 
. 
Common 
{ 
public 

class 
DateTimeHelper 
{ 
private		 
static		 
readonly		 
TimeZoneInfo		  ,
SpecifiedTimeZone		- >
;		> ?
static

 
DateTimeHelper

 
(

 
)

 
{ 	
try 
{ 
if 
(  
ConfigurationManager (
.( )
AppSettings) 4
[4 5
$str5 ?
]? @
!=A C
nullD H
)H I
{ 
SpecifiedTimeZone %
=& '
TimeZoneInfo( 4
.4 5"
FindSystemTimeZoneById5 K
(K L 
ConfigurationManagerL `
.` a
AppSettingsa l
[l m
$strm w
]w x
)x y
;y z
Logger 
. 
Info 
(  
$str  2
+3 4
SpecifiedTimeZone5 F
.F G
IdG I
)I J
;J K
} 
else 
{ 
Logger 
. 
Info 
(  
$str  7
)7 8
;8 9
} 
} 
catch 
( 
	Exception 
ex 
)  
{ 
Logger 
. 
Warn 
( 
$str M
+N O
exP R
.R S
MessageS Z
)Z [
;[ \
} 
SpecifiedTimeZone 
= 
TimeZoneInfo  ,
., -
Local- 2
;2 3
} 	
public!! 
static!! 
DateTime!! (
DateTimeLocalToSpecifiedZone!! ;
(!!; <
System!!< B
.!!B C
DateTime!!C K
dt!!L N
)!!N O
{"" 	
return## 
TimeZoneInfo## 
.##  
ConvertTimeFromUtc##  2
(##2 3
TimeZoneInfo##3 ?
.##? @
ConvertTimeToUtc##@ P
(##P Q
dt##Q S
)##S T
,##T U
SpecifiedTimeZone##V g
)##g h
;##h i
}$$ 	
public%% 
static%% 
DateTime%% 
ZoneNow%% &
{&& 	
get'' 
{(( 
return)) 
TimeZoneInfo)) #
.))# $
ConvertTimeFromUtc))$ 6
())6 7
System))7 =
.))= >
DateTime))> F
.))F G
UtcNow))G M
,))M N
SpecifiedTimeZone))O `
)))` a
;))a b
}** 
},, 	
}-- 
}.. ù
rC:\Users\hw72786\Source\Repos\OverWatcher\OverWatcher\OverWatcher.Common.DateTimeHelper\Properties\AssemblyInfo.cs
[ 
assembly 	
:	 

AssemblyTitle 
( 
$str <
)< =
]= >
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
$str >
)> ?
]? @
[ 
assembly 	
:	 

AssemblyCopyright 
( 
$str 0
)0 1
]1 2
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
[ 
assembly 	
:	 


ComVisible 
( 
false 
) 
] 
[ 
assembly 	
:	 

Guid 
( 
$str 6
)6 7
]7 8
[## 
assembly## 	
:##	 

AssemblyVersion## 
(## 
$str## $
)##$ %
]##% &
[$$ 
assembly$$ 	
:$$	 

AssemblyFileVersion$$ 
($$ 
$str$$ (
)$$( )
]$$) *