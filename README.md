# Caminos Patrimoniales

App móvil Android de turismo histórico con realidad aumentada (RA), desarrollada en el semillero **GeoGeeks de Esri Colombia**, inspirada en el proyecto GeoTrails (CUE24).

Permite al usuario seguir recorridos históricos en primera persona: puntos guía proyectados sobre el suelo, overlay de instrucciones, minimapa con calles reales, y pines 3D en los puntos de interés (POIs) con contenido histórico cargado desde ArcGIS Online.

## Estado actual

En desarrollo activo: Módulo de navegación AR y POIs funcionando; ruta real caminable y secuencialidad completa de los 8 POIs en construcción. Ver `docs/ARCHITECTURE.md` para el detalle de cada módulo y `docs/CHANGELOG.md` (opcional, ver abajo) para el histórico semana a semana.

## Stack tecnológico

| Componente | Versión |
|---|---|
| Motor | Unity 6.3 LTS (6000.3.15f1) |
| GIS | ArcGIS Maps SDK for Unity 2.3.0 |
| AR | AR Foundation 6.3.4 + Google ARCore XR Plugin |
| Backend de datos | ArcGIS Online (Feature Services) |
| Lenguaje | C# |
| Plataforma | Android (ARM64), min API 28 |

## Requisitos para levantar el proyecto

- Unity 6.3 LTS (6000.3.15f1) — instalar manualmente si Kaspersky u otro antivirus corporativo bloquea el instalador de Unity Hub.
- Android SDK (platform 35), NDK r27c (27.2.12479018), JDK 17.
- Una **API key de ArcGIS propia**
- Descargar por separado el **Projection Engine Data** del ArcGIS Maps SDK (no se versiona por su tamaño) y apuntar la ruta en el componente `ArcGIS Map` de la escena `ARNavigation`.

## Estructura del repositorio

```
Assets/
├── Scripts/          → todo el código C# (ver docs/ARCHITECTURE.md)
├── Prefabs/           → WaypointMarker, POIPin, RouteCard, POIListItem
├── Materials/
├── Textures/
├── Scenes/            → SplashScreen, RouteList, RoutePreview, ARNavigation
docs/
├── ARCHITECTURE.md    → explicación de cada script y cómo se relacionan
└── SETUP.md           → (opcional) pasos detallados de instalación del entorno
```

## Los 8 POIs del recorrido piloto (La Candelaria, Bogotá)

Tema: patrimonio hídrico. Datos servidos desde ArcGIS Online (`POIs_CaminosPatrimoniales` FeatureServer).

1. Museo de Bogotá - Entrada
2. Museo de Bogotá - Interior 1
3. Museo de Bogotá - Interior 2
4. Calle de la Toma del Agua
5. Estación Acueducto de Bogotá
6. Acueducto de Bogotá
7. Calle de las Violetas
8. Eje Ambiental
