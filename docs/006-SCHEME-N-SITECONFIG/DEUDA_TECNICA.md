- Nota: Se mantiene Content.SiteId (un solo sitio legacy) en el modelo; la lista que usa la UI y la API es la de ContentSite (SiteIds). La publicación sigue siendo global (no por sitio); si más adelante quieres “publicado en sitios [ids]”, haría falta otra fase (estado o tabla de publicación por ContentId + SiteId).

- Nota: La jerarquía de contenido (ParentContentId en Content) se mantiene como está; no se ha eliminado. Si quieres quitarla y usar solo categorías (Jerarquías) para agrupar, se puede hacer en un siguiente paso.

