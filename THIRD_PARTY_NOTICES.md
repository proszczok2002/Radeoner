# Third-party notices / Informacje o składnikach zewnętrznych

Optional RGB support uses an independently implemented client of the public
[OpenRGB SDK protocol](https://github.com/CalcProgrammer1/OpenRGB/blob/master/Documentation/OpenRGBSDK.md).
OpenRGB is an external application; no OpenRGB source, executable, library or
hardware-access driver is redistributed in this repository or portable package.
Its names and protocol identifiers are used for interoperability.

Opcjonalna obsługa RGB korzysta z niezależnie zaimplementowanego klienta
publicznego protokołu SDK OpenRGB. OpenRGB jest zewnętrznym programem;
nie dołączamy jego kodu, binariów, bibliotek ani sterowników sprzętowych.
Nazwy i identyfikatory protokołu służą zgodności komunikacji.

Radeoner's application source is published under the MIT license in `LICENSE`.
The repository and release packages do not redistribute the AMD ADLX SDK,
AMD sample implementations or AMD driver binaries. Radeoner defines the native
interop signatures and interface slot numbers needed to call the installed
driver's public ADLX API. The original API names and ABI identifiers are
retained for interoperability.

Kod aplikacji Radeoner jest udostępniony na licencji MIT w pliku `LICENSE`.
Repozytorium i paczki nie zawierają SDK ADLX, implementacji przykładów AMD
ani plików binarnych sterownika. Sygnatury i numery metod służą komunikacji
z publicznym API ADLX zainstalowanego sterownika. Nazwy API i identyfikatory ABI
zachowano dla zgodności.

Optional developer reference: [AMD ADLX](https://github.com/GPUOpen-LibrariesAndSDKs/ADLX).
If you obtain the SDK separately, its own license applies to that SDK. It is
not covered by Radeoner's MIT license and is not required to build the
committed application source.

Opcjonalne materiały dla programistów: AMD ADLX pod powyższym adresem.
Pobrane oddzielnie SDK podlega własnej licencji AMD. Nie obejmuje go MIT
Radeonera i nie jest potrzebne do kompilacji zatwierdzonych źródeł aplikacji.

AMD, Radeon and related marks belong to Advanced Micro Devices, Inc.
Windows, Microsoft and .NET are trademarks of Microsoft Corporation.
Radeoner is an independent community project, not an AMD or Microsoft product.

AMD i Radeon oraz powiązane znaki należą do Advanced Micro Devices, Inc.
Windows, Microsoft i .NET należą do Microsoft Corporation. Radeoner jest
niezależnym projektem społecznościowym, nie produktem AMD ani Microsoft.
