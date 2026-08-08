# Proyecto PEMEX

## Digitalización y gestión de órdenes de trabajo

Sistema de escritorio desarrollado para **digitalizar el proceso de gestión de órdenes de trabajo**, reduciendo la dependencia de documentos impresos y el consumo de papel generado por el llenado, modificación, firma y almacenamiento físico de hojas.

El proyecto busca transformar un proceso tradicional basado en **llenar, imprimir, firmar y archivar documentos** en un flujo de trabajo digital mediante archivos PDF.

> **Nota:** este es un prototipo funcional desarrollado durante prácticas profesionales en PEMEX TAD Miahuatlán. No se implementó en producción debido a restricciones de infraestructura de red institucional (red privada sin acceso habilitado para el despliegue). El proyecto se entregó junto con un reporte técnico a la jefatura correspondiente.

### Objetivo

El objetivo principal del proyecto es **reducir el uso de papel y facilitar la gestión documental de las órdenes de trabajo**, permitiendo realizar digitalmente actividades que anteriormente requerían el uso de documentos físicos.

Entre los principales beneficios se encuentran:

* Reducir la cantidad de hojas impresas.
* Evitar impresiones innecesarias por correcciones o modificaciones.
* Digitalizar el llenado de información.
* Facilitar la incorporación de firmas.
* Registrar el cierre de órdenes de trabajo directamente en el documento.
* Facilitar la búsqueda y consulta de órdenes.
* Centralizar los documentos digitales.
* Reducir el tiempo empleado en la búsqueda manual de documentos físicos.
* Facilitar la organización y clasificación de las órdenes de trabajo.

## Descripción

El sistema está compuesto por dos módulos que trabajan sobre el mismo flujo documental:

### Editor de PDF — C# / WPF

Aplicación de escritorio desarrollada en **C# con WPF**, encargada de trabajar directamente sobre las órdenes de trabajo en formato PDF.

Permite:

* Abrir documentos PDF.
* Visualizar sus páginas.
* Navegar entre páginas.
* Utilizar zoom para trabajar sobre el documento.
* Insertar texto directamente sobre el PDF.
* Configurar fuente, tamaño y color del texto.
* Insertar firmas.
* Colocar el sello **"CERRADO PM SAP"**.
* Guardar el documento modificado.
* Deshacer y rehacer acciones.

De esta manera, modificaciones que anteriormente podían requerir volver a imprimir una hoja pueden realizarse directamente sobre el documento digital.

### Gestor de PDFs — Python

Aplicación desarrollada en **Python y Tkinter** para facilitar la organización, búsqueda y consulta de las órdenes de trabajo almacenadas digitalmente.

Entre sus funciones se encuentran:

* Cargar una carpeta de documentos PDF.
* Analizar automáticamente el contenido de los documentos.
* Extraer información relevante.
* Identificar descripción de la orden.
* Identificar operadores.
* Detectar fechas de inicio.
* Detectar si una orden se encuentra cerrada.
* Detectar y mostrar observaciones.
* Buscar órdenes mediante texto.
* Filtrar por descripción.
* Filtrar por operador.
* Filtrar por fecha.
* Ordenar los resultados.
* Previsualizar documentos.
* Abrir los PDFs directamente.

Esto permite sustituir la búsqueda manual entre documentos físicos por una consulta digital más rápida y organizada.

## Flujo general

```text
          ORDEN DE TRABAJO
                 │
                 ▼
        ┌─────────────────┐
        │  Documento PDF  │
        └────────┬────────┘
                 │
                 ▼
       ┌─────────────────────┐
       │   Editor de PDF     │
       │      C# / WPF       │
       └─────────┬───────────┘
                 │
       ┌─────────┼─────────┐
       ▼         ▼         ▼
     Texto     Firma    Cierre
       │         │         │
       └─────────┼─────────┘
                 ▼
          PDF actualizado
                 │
                 ▼
       ┌─────────────────────┐
       │   Gestor de PDFs    │
       │      Python         │
       └─────────┬───────────┘
                 │
       ┌─────────┼───────────┐
       ▼         ▼           ▼
    Buscar    Filtrar    Clasificar
       │         │           │
       └─────────┼───────────┘
                 ▼
          Consulta digital
```

## Tecnologías utilizadas

### Aplicación principal

* **C#**
* **WPF**
* **.NET Framework 4.7.2**
* Entity Framework 6.5.1
* iText 9
* PDFsharp
* PdfiumViewer
* Magick.NET
* BCrypt.Net-Next
* BouncyCastle
* Newtonsoft.Json

### Gestor documental

* **Python**
* Tkinter
* PyMuPDF (`fitz`)
* tkcalendar
* Pillow
* JSON
* `re`
* `threading`
* `subprocess`

## Funcionalidades principales

| Área           | Funcionalidad                                 |
| -------------- | --------------------------------------------- |
| Edición        | Inserción de texto en PDF                     |
| Firmas         | Inserción de firma                            |
| Cierre         | Sello "CERRADO PM SAP"                        |
| Visualización  | Renderizado y navegación de páginas           |
| Zoom           | Ampliación y reducción del documento          |
| Historial      | Deshacer y rehacer                            |
| Gestión        | Organización de órdenes                       |
| Búsqueda       | Búsqueda por contenido                        |
| Filtros        | Descripción, operador y fecha                 |
| Observaciones  | Detección y consulta                          |
| Digitalización | Reducción de impresiones y documentos físicos |

## Estructura del proyecto

```text
Proyecto-PEMEX/
│
├── Editor PDFs/
│   └── WpfApp2/
│       ├── App.xaml
│       ├── MainWindow.xaml
│       ├── MainWindow.xaml.cs
│       ├── WpfApp2.csproj
│       └── ...
│
└── Gestor PDFs/
    └── app.py
```

## Requisitos

* Windows
* Visual Studio
* .NET Framework 4.7.2
* Python 3.x
* Paquetes NuGet del proyecto
* Dependencias de Python
* [Ghostscript](https://www.ghostscript.com/releases/gsdnld.html) (requerido por Magick.NET para procesar PDFs)

## Instalación

Clona el repositorio:

```bash
git clone https://github.com/D1eg0PH/Proyecto-PEMEX.git
```

### Editor de PDF (C#)

Abrir la solución del proyecto en Visual Studio, restaurar los paquetes NuGet y ejecutar la aplicación.

### Gestor de PDFs (Python)

Instalar las dependencias:

```bash
pip install pymupdf pillow tkcalendar
```

Ejecutar:

```bash
python "Gestor PDFs/app.py"
```

## Impacto esperado

El proyecto busca generar una mejora principalmente en tres áreas:

### Menor consumo de papel

Al permitir que el llenado, modificación, firma y cierre de órdenes se realicen digitalmente, se reduce la necesidad de imprimir documentos para realizar modificaciones o completar información.

### Mayor eficiencia

La búsqueda y consulta digital permite localizar órdenes de trabajo sin depender de archivos físicos y búsquedas manuales.

### Mejor gestión documental

Los documentos pueden mantenerse organizados digitalmente, facilitando su consulta y seguimiento.

## Alcance

El proyecto representa un **prototipo de escritorio** orientado a la digitalización de procesos documentales, utilizando PDF como formato principal para conservar la información de las órdenes de trabajo. Se validó su funcionamiento a nivel local; el despliegue en el entorno de producción de PEMEX quedó pendiente por las restricciones de infraestructura mencionadas arriba.

## Autor

**Diego Armando Pérez Huerta**

Proyecto académico / profesional enfocado en la digitalización y optimización de procesos documentales.
