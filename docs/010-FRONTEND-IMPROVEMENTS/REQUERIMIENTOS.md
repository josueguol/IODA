
# FRONTEND

## Al ri a la seccion de esquemas y cuando no hay schemas

- Si no hay schemas deberia verse como en el frame ["001 - Schemas editor" "Node ID: L6TZA"] @docs/010-FRONTEND-IMPROVEMENTS/create-schema.pen la cual esta basada en los steps de creacion de proyectos, entornos y sitios.
- El menú de Dashboard, contenido, publicar debe cambiar a "configuración de sitio", Schemas, Jerarquías, Etiquetas.
- De igual manera para poder regresar al menú anterior en Settings hará falta agregar un elemento llamado Dashboar debajo de él un separados y los demas elementos existente tal como de dibuja en el menu bajo settings en ["001 - Schemas editor" "Node ID: L6TZA"] @docs/010-FRONTEND-IMPROVEMENTS/create-schema.pen 

## Cuando voy a la seccion schemas, puedo crear, pero no puedo editar los existentes.

- Si edito, no todos los campos deben pueden ser editados.
  - Slug de esquema, se convierte en el identificador unico (por eso el usuario debe tener cuidado al generarlo).

## Hay campos que no sirven o no tiene sentido su existencia

- "Hereda de (opcional)": no le veo utilidad, ni sentido, ayudame a justificar su existencia o eliminalo.
- El campo de "Orden" es un campo interno, no tiene utilidad, si deseo ordenar será por algún otro campo.

## No entiendo como es que puedo agregar tres tipos de bloques

- hero
- texto
- imagen

¿A qué corresponde cada uno?

Realizar analisis para ver si lo quitamos o cambiamos la lógica de lo esperado.

NOTA: me parece que no fue bien entendida la solucitud, lo que requiero es poder configurar bloques ya sea dentro de contenido(richtext) o para el esquema(schema), los bloques de contenido, pueden servir para configurar listas dinamicas de contenido, componentes provenientes de themas (requiere el modulo de themes y desarrollo de como será la lógica y comunicacion entre ambos mundos), tambien pueden ser embeds, etc.

## Cuando creo un schema ["002 - Schemas designer" "Node ID: AFKLa"]

- Los campos recomendados deberias estar agregados y no insertarse al presionar "Sugerir campos por defecto ("Descripción corta", contenido)", el usuario decide si los deja o los remueve.
- Los campos nativos de schema como título y slug no pueden eliminarse; los personalizados y recomendados, sí. Todos pueden cambiarse de orden en el editor de schemas.
- Quitar título de los campos recomendados: ya existe uno nativo de squema para titulo.
  - Título del contenido * (este ya puede servir como titulo de la nota, y eliminamos el otro que es custom y se llama titulo, también, dejar solo "Descripción corta" y "Contenido" como recomendados)
- El diseñador de squemas debe verse organizarse como en la vista ["002 - Schemas designer" "Node ID: AFKLa"] recuerda que es responsibe, firstmobile.
  - Los campos que se pueden agregar a la izquierda.
  - La vista previa en medio
  - propiedades del squema y configuración de campos de lado derecho
