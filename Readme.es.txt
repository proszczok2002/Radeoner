Radeoner 1.06 — guía del usuario

Radeoner es un panel AMD ligero y portátil para Windows 10/11 x64 con Microsoft
.NET Framework 4.8. Basta el controlador AMD con ADLX; no requiere el panel
Adrenalin. Ejecute Radeoner.exe desde una carpeta con permiso de escritura.
No instala servicios adicionales para ajustar la GPU.

IDIOMA Y APARIENCIA
Ajustes > Idioma: Polski, English, Deutsch, Русский, Español o Français.
Temas: sistema, claro y oscuro. Tamaño de texto: 100–200 %. Los cambios son
inmediatos. F1 ofrece esta guía sin conexión. Las traducciones están en archivos
UTF-8 lang/*.lng del código público. Los idiomas nuevos van integrados en el EXE;
no requieren instalar paquetes por separado.

FUNCIONES SEGÚN LA GPU
Seleccione la GPU en el listado superior. Lecturas, Informe y Ajustes siguen
disponibles. Ajustes GPU y Ventilador aparecen cuando hay funciones apropiadas;
las tarjetas dedicadas también ofrecen acceso a RGB opcional. Los voltajes,
frecuencias y curvas no compatibles no aparecen como campos editables. Una
GPU integrada puede admitir algunas funciones; estas siguen disponibles.
El firmware puede controlar los ventiladores del portátil. En la bandeja, una
GPU integrada no menciona ventilador GPU. En otras GPU se muestran RPM solo
con una lectura válida; 0 RPM es válido e indica parada.

TECLADO Y NVDA
Tab / Mayús+Tab: controles. Ctrl+Tab / Ctrl+Mayús+Tab: pestaña disponible
siguiente/anterior. Ctrl+1–5: Lecturas, Ajustes GPU, Ventilador, Informe,
Ajustes. Las funciones no disponibles se omiten. F5 actualiza los parámetros.
F6 selecciona y lee el estado. F1 abre la ayuda. Introduzca números o use
flechas arriba/abajo. Cada campo contiene nombre, unidad, valor e intervalo.
En Lecturas, flechas seleccionan filas, Ctrl+A selecciona todo y Ctrl+C copia.
Alt+F4 oculta en la bandeja; Salir termina la aplicación por completo.
Windows+B lleva al área de notificaciones; seleccione Radeoner y pulse Intro
para abrir. El menú del icono también permite Salir.

CAMBIAR AJUSTES
Revise valores, edítelos y pulse Aplicar o Ctrl+Intro. Solo la confirmación
escribe en la GPU. Iniciar o leer no cambia parámetros. El controlador aporta
intervalos y pasos. En Navi4 o posteriores puede tratarse de desplazamientos
de frecuencia/voltaje, no valores absolutos: lea la descripción. Un límite
porcentual de potencia no es TDP en vatios y no cambia automáticamente el
límite compartido CPU/iGPU. Valores inadecuados pueden causar inestabilidad.
Las temperaturas de curva deben crecer y las velocidades no disminuir.
Valores de fábrica restablece, tras confirmar, también cambios de otras
aplicaciones. Deshacer está disponible tras un cambio escrito y confirmado.

PERFILES E INFORME
Ctrl+E exporta a JSON valores del editor, incluso sin aplicar. Ctrl+I importa
solo al editor. Se comprueban identidad de GPU, intervalos, pasos, unidades
y curvas. Para escribir, confirme Aplicar de nuevo. Conserve copias de los
perfiles y use hardware compatible. Los perfiles ADLX no incluyen RGB.
Ctrl+Mayús+S guarda el informe de texto. Revíselo antes de publicarlo: puede
incluir identificadores de hardware.

RGB
Ajustes GPU > Iluminación RGB. Actualmente requiere un servidor SDK OpenRGB
local; esta versión no incorpora una implementación de hardware independiente.
Descargar OpenRGB guarda un ZIP oficial verificado. Extráigalo, ejecute
OpenRGB.exe y active el servidor SDK, normalmente puerto 6742. Conecte,
seleccione GPU, modo, color y brillo y pulse Aplicar RGB. Apagar o 0 % apaga
la luz. La compatibilidad de modelos depende de OpenRGB. Su programa y sus
controladores de hardware no se ejecutan ni instalan automáticamente.

INICIO, ACTUALIZACIONES Y ERRORES
El inicio automático abre la bandeja al iniciar sesión del usuario actual.
Configure comprobaciones automáticas, intervalo en minutos y al iniciar.
Las novedades se muestran antes de instalar; Sí descarga, verifica e instala,
No cancela. Se conservan perfiles y preferencias. El EXE anterior se elimina
tras el inicio correcto; si la nueva versión no arranca, se restaura la anterior.
Un fallo de controlador deja abierta la ventana y reintenta lecturas, nunca
escrituras automáticamente. Use F5 y compruebe los valores reales. Los detalles
de errores locales están en Radeoner.log.

CÓDIGO, CONTACTO Y APOYO VOLUNTARIO
Licencia MIT: https://github.com/proszczok2002/Radeoner
Versiones: https://github.com/proszczok2002/Radeoner/releases
Autor: https://proszczok.pl/ — Telegram: https://t.me/proszczok
PayPal: https://paypal.me/proszczok — Revolut: https://revolut.me/r_rosiejka
Los enlaces están en Ajustes > Acerca de / Apoyo y contacto. Para informar
de un problema, indique versiones de aplicación, Windows, NVDA, modelo GPU y pasos.
