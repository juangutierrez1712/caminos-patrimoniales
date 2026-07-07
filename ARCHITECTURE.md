# Arquitectura del proyecto

Este documento explica qué hace cada script y cómo se conectan entre sí. Está pensado para que un desarrollador nuevo entienda el flujo sin tener que leer todo el código primero.

## Flujo general de escenas

```
SplashScreen → RouteList → RoutePreview → ARNavigation
```

`SceneLoader.cs` es el único punto de navegación entre escenas. Es un singleton con `DontDestroyOnLoad` que además guarda `SelectedRoute` (el recorrido elegido) como propiedad estática, para que sobreviva el cambio de escena sin depender de `PlayerPrefs` ni de pasar datos por Unity.

## Módulo de ubicación (GPS + brújula)

**`LocationManager.cs`** — Singleton persistente. Es la única fuente de verdad de la posición del usuario en todo el proyecto. Al iniciar, pide permiso de ubicación fina en Android, arranca `Input.location` y activa `Input.compass`. Expone `Latitude`, `Longitude`, `Accuracy`, `Heading` y una bandera `IsReady` que el resto de scripts deben chequear antes de leer cualquier dato — mientras el GPS no responde, estos valores no son confiables.

**`DebugGPS.cs`** — Panel de texto en pantalla que muestra en vivo lat/lon/precisión/brújula y el estado del `PathProjector` y del `POIManager`. Es una herramienta de depuración, no forma parte del flujo de usuario final; se puede desactivar o quitar del Canvas antes de una build de producción.

## Módulo de navegación AR (guía hacia el POI)

**`PathProjector.cs`** — Calcula distancia y *bearing* (rumbo) real entre la posición del usuario y el POI activo (leído de `POIManager.Instance.CurrentPOI`), usando la fórmula de Haversine. Con esa dirección, coloca una fila de puntos guía (`dots`) frente a la cámara, más pequeños cuanto más lejos, simulando una línea que se aleja hacia el POI. El ángulo se recalcula restando el heading de la brújula al bearing, para que los puntos apunten en la dirección correcta sin importar hacia dónde mire el usuario.

**`NavigationOverlay.cs`** — Overlay fijo de texto (distancia + instrucción) que no depende de hacia dónde apunte la cámara, porque vive en el Canvas en modo *Screen Space - Overlay*. Cambia el mensaje según la distancia al POI actual y se oculta automáticamente si `POIPanel` está abierto, para no superponerse.

**`PlayerDotRotator.cs`** — Rota el ícono del jugador en el minimapa según el heading de la brújula, para que siempre se vea hacia dónde está mirando.

## Módulo de minimapa

**`MinimapWebTile.cs`** — Descarga tiles reales de OpenStreetMap (zoom 17, nivel calle) centrados en la posición GPS actual y los muestra en un `RawImage`. Solo pide un tile nuevo si el usuario se movió más de 5 metros, para no saturar de peticiones de red.

**`MinimapFollow.cs`** / **`MarkerFollow.cs`** — Scripts de cámara/objeto que siguen al jugador en el plano XZ mientras mantienen una altura fija; se usaban en la versión anterior del minimapa con cámara ortográfica 3D (antes de migrar a tiles de OSM). Revisar si siguen en uso o son remanentes de la versión anterior.

**`MinimapWebTile.cs`** reemplazó ese enfoque por tiles reales; si `MinimapFollow`/`MarkerFollow` ya no se usan en la jerarquía activa, considerar moverlos a una carpeta `Deprecated/` o eliminarlos para no confundir al siguiente desarrollador.

## Módulo de POIs (puntos de interés)

**`POIData.cs`** — Clase de datos simple (sin `MonoBehaviour`) que modela un POI: nombre, descripción, historia, foto y coordenadas.

**`POIManager.cs`** — Singleton persistente. Descarga los 8 POIs desde el Feature Service de ArcGIS Online al arrancar (`&outSR=4326` es obligatorio en la URL para recibir coordenadas en grados decimales y no en Web Mercator). Expone la lista completa, el POI activo (`CurrentPOI`) y métodos de navegación secuencial (`NextPOI`, `PreviousPOI`, `ResetToFirstPOI`). Dispara los eventos `OnPOIsLoaded` y `OnPOIChanged` para que otros scripts (pin, panel, overlay) reaccionen sin necesidad de sondear el estado en cada frame.

**`POIPinController.cs`** — Muestra un pin 3D cuando el usuario entra en `activationRadius` metros del POI activo, lo posiciona según el bearing real (igual que `PathProjector`), y detecta el toque sobre el pin vía raycast para abrir `POIPanel`. **Importante:** este radio suele quedar en un valor grande (miles de metros) durante pruebas remotas; debe volver a ~20m antes de cualquier prueba de campo real.

**`POIPinPulse.cs`** — Efecto visual de pulso (escala oscilante) en la cabeza del pin, puramente estético.

**`POIMarkerFollow.cs`** — Posiciona el marcador del POI activo en el minimapa, convirtiendo la diferencia de grados lat/lon a metros relativos a la cámara.

**`POIPanel.cs`** — Bottom sheet con nombre, historia/descripción y foto (cargada por URL) del POI activo. Los botones "Anterior"/"Siguiente" llaman a `POIManager` y cierran el panel; el estado de los botones (habilitado/deshabilitado) refleja si es el primer o último POI del recorrido.

## Módulo de selección de recorrido

**`RouteData.cs`** — Modelo de datos de un recorrido (nombre, duración, distancia, dificultad, URLs de los Feature Services de POIs y de ruta).

**`RouteListController.cs`** — Genera las tarjetas de recorridos disponibles. Actualmente usa datos hardcodeados (un solo recorrido piloto); en el futuro debería cargar esta lista desde AGOL igual que hace con los POIs.

**`RouteCard.cs`** — Puebla una tarjeta individual y navega a `RoutePreview` al seleccionarla.

**`RoutePreviewController.cs`** — Muestra el resumen del recorrido elegido y la lista ordenada de sus POIs (consultando el Feature Service correspondiente). El botón "Iniciar" resetea `POIManager` al primer POI y navega a `ARNavigation`.
