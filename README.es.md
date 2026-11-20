# Radeoner

**1.08** muestra todos los cambios en la ventana de actualización y busca controladores en las páginas de productos de AMD. [Cambios](docs/releases/v1.08.md#español)

Panel AMD GPU ligero y portátil para Windows, con controles nativos de
Windows Forms, manejo por teclado y compatibilidad con NVDA.

[Polski](README.pl.md) · [English](README.md) · [Deutsch](README.de.md) ·
[Русский](README.ru.md) · **Español** · [Français](README.fr.md)

## Descargar y ejecutar

1. Descargue el ZIP portátil de [versiones](https://github.com/proszczok2002/Radeoner/releases/latest).
2. Extraiga todos los archivos en una carpeta con permiso de escritura.
3. Ejecute **Radeoner.exe**. No requiere instalador.

Requisitos: Windows 10/11 x64, Microsoft .NET Framework 4.8 y un controlador AMD
con ADLX. La instalación Driver Only sirve si incluye ADLX; no se necesita
el panel Adrenalin. La biblioteca del controlador se carga únicamente desde
System32. Radeoner no instala un controlador de núcleo propio para la GPU.

Para Radeoner recomendamos **Driver Only** en las opciones adicionales del instalador de AMD. Si mantiene el panel Adrenalin, ajuste la GPU con una sola aplicación. Deje Factory Reset desactivado en una actualización normal. En una reinstalación limpia, esa opción elimina los paquetes anteriores y suprime la posibilidad de volver al controlador previo. En portátiles, consulte también al fabricante.
[Controladores AMD y GPU antiguas](docs/AMD_DRIVERS.md) · [AMD](https://www.amd.com/en/resources/support-articles/faqs/RSX2-INSTALL.html)

Selecciona tu GPU y abre **Ajustes → Actualizaciones → Actualizar controlador AMD**. Consulta la página del producto, lee la licencia de AMD y descarga el paquete completo para verificarlo. Radeoner valida la firma y Windows identifica el INF gráfico compatible. Puedes instalar un controlador más reciente tras confirmar los permisos de administrador. La ventana de actualización de Radeoner muestra todos los cambios en un campo de solo lectura que recibe el foco al abrirse.

## Funciones y versión 1.06

- Las lecturas se actualizan cada segundo por defecto, incluso en la bandeja.
- Frecuencias, voltaje, límite de potencia, curvas del ventilador y Zero RPM
  aparecen según lo que admita el controlador. Las funciones compatibles de
  una GPU integrada se conservan; las pestañas vacías se ocultan.
- El icono de una GPU integrada no menciona ventiladores. Para otras GPU solo
  muestra RPM válidas, incluido el valor válido de 0 RPM.
- Copias de perfiles JSON. Importar solo rellena los campos; **Aplicar**
  escribe en la GPU después de confirmar los cambios.
- Seis idiomas, temas del sistema/claro/oscuro y tamaño del texto 100–200%.
- **F1** abre la guía completa traducida sin conexión a internet.
- Búsqueda opcional de actualizaciones al inicio y por intervalo. Se muestran
  los cambios antes de instalar; los perfiles y preferencias se conservan.

RGB sigue requiriendo un **servidor SDK OpenRGB local** opcional. Radeoner
puede descargar el ZIP oficial verificado. Extraer, ejecutar OpenRGB y activar
su servidor SDK son pasos separados. La versión 1.06 no incluye acceso nativo
independiente a los controladores de iluminación.

## Teclado y NVDA

| Teclas | Acción |
| --- | --- |
| Tab / Mayús+Tab | Campo siguiente / anterior |
| Ctrl+Tab / Ctrl+Mayús+Tab | Pestaña disponible siguiente / anterior |
| Ctrl+1…Ctrl+5 | Lecturas, Ajustes GPU, Ventilador, Informe, Ajustes |
| F5 / F6 / F1 | Actualizar / seleccionar estado / ayuda |
| Ctrl+E / Ctrl+I | Exportar / importar perfil |
| Ctrl+Intro | Aplicar cambios |
| Ctrl+Mayús+S | Guardar informe |
| Alt+F4 | Ocultar en la bandeja |

Un destino Ctrl+1…5 no disponible vuelve a Lecturas. Use **Salir** para cerrar
el programa por completo. Windows+B permite llegar al icono; Intro abre la
ventana. En la lista de lecturas, las flechas seleccionan filas y Ctrl+A/Ctrl+C
seleccionan y copian los valores.

## Límites, archivos y colaboración

El límite porcentual de potencia no es TDP en vatios ni el límite conjunto
CPU/iGPU. Algunos valores de Navi4 son desplazamientos; lea la descripción.
Radeoner no desbloquea el firmware ni controla todos los ventiladores de un
portátil. La escritura física y el RGB deben verificarse en cada modelo.
Las pruebas automáticas no sustituyen la lectura real de NVDA. El
[informe de comprobación](docs/AUDIT_1.06.md) distingue estas pruebas.

Los seis idiomas y guías están integrados en el EXE. Un archivo UTF-8 local
`lang/<código>.lng` puede sustituir los textos. Los ocho archivos del ZIP
mantienen la compatibilidad con el actualizador 1.05. Las preferencias están
en `Radeoner.settings.json` junto al EXE. Los perfiles de GPU se exportan
por separado y no se aplican automáticamente al iniciar.

Se verificó la actualización **1.07 → 1.08** con el paquete local y el canal
público de GitHub. Se conservaron los perfiles y las preferencias. [Resultado](docs/UPDATE_TEST_1.08.md#español)

[Guía completa](Readme.es.txt) · [Cambios 1.06](docs/releases/v1.06.md#español)

El código es público bajo la licencia **MIT**. Compilación local:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

[Comunique errores](https://github.com/proszczok2002/Radeoner/issues) o envíe
pull requests indicando modelo de GPU y versiones de Windows, controlador,
programa y NVDA. Los nombres MSAA por sí solos no confirman la lectura de NVDA.

Autor: [proszczok.pl](https://proszczok.pl/); contacto: [Telegram](https://t.me/proszczok).
Apoyo voluntario: [PayPal](https://paypal.me/proszczok),
[Revolut](https://revolut.me/r_rosiejka).
