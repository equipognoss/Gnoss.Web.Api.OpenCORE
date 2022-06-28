![](https://content.gnoss.ws/imagenes/proyectos/personalizacion/7e72bf14-28b9-4beb-82f8-e32a3b49d9d3/cms/logognossazulprincipal.png)

# Gnoss.Web.Api.OpenCORE

Aplicación Web que ofrece un interfaz de programación para que otras aplicaciones puedan realizar consultas o modificaciones en los datos almacenados en la plataforma de manera automatizada. Permite crear y gestionar comunidades, recursos, usuarios, etc.

Esta aplicación está protegida por OAuth 1.0 y cualquier petición que se realice a ella debe ir firmada bajo ese protocolo. 

Configuración estandar de esta aplicación en el archivo docker-compose.yml: 

```yml
api:
    image: gnoss/api
    env_file: .env
    ports:
     - ${puerto_api}:80
    environment:
     acid: ${acid}
     base: ${base}
     oauth: ${oauth}
     virtuosoConnectionString: ${virtuosoConnectionString}
     redis__redis__ip__master: ${redis__redis__ip__master}
     redis__redis__ip__read: ${redis__redis__ip__read}
     redis__redis__bd: ${redis__redis__bd}
     redis__redis__timeout: ${redis__redis__timeout}
     redis__recursos__ip__master: ${redis__recursos__ip__master}
     redis__recursos__ip__read: ${redis__recursos__ip__read}
     redis__recursos__bd: ${redis__recursos__bd}
     redis__recursos__timeout: ${redis__redis__timeout}
     redis__liveUsuarios__ip__master: ${redis__liveUsuarios__ip__master}
     redis__liveUsuarios__ip__read: ${redis__liveUsuarios__ip__read}
     redis__liveUsuarios__bd: ${redis__liveUsuarios__bd}
     redis__liveUsuarios__timeout: ${redis__redis__timeout}
     RabbitMQ__colaReplicacion: ${RabbitMQ}
     RabbitMQ__colaServiciosWin: ${RabbitMQ}
     idiomas: ${idiomas}
     IpServicioSocketsOffline: ${IpServicioSocketsOffline}                                                  
     PuertoServicioSocketsOffline: ${puerto_ServicioSocketsOffline}                                               
     Servicios__autocompletar: ${Servicios__autocompletar}
     Servicios__urlInterno: "http://interno"
     Servicios__urlArchivos: ${Servicios__urlArchivos}
     Servicios__urlDocuments: "http://documents/GestorDocumental"
     Servicios__urlContent: ${Servicios__urlContent}
     Servicios__urlOauth: "http://oauth/"
     connectionType: ${connectionType}
     Virtuoso__Escritura__VirtuosoLecturaPruebasGnoss_v3: ${Virtuoso__Escritura__VirtuosoLecturaPruebasGnoss_v3}
     Virtuoso__Escritura__VirtuosoLecturaPruebasGnoss_v4: ${Virtuoso__Escritura__VirtuosoLecturaPruebasGnoss_v4}
     BidirectionalReplication__VirtuosoLecturaPruebasGnoss_v3: ${BidirectionalReplication__VirtuosoLecturaPruebasGnoss_v3}
     BidirectionalReplication__VirtuosoLecturaPruebasGnoss_v4: ${BidirectionalReplication__VirtuosoLecturaPruebasGnoss_v3}
    volumes:
      - ./logs/api:/app/logs

```

Se pueden consultar los posibles valores de configuración de cada parámetro aquí: https://github.com/equipognoss/Gnoss.SemanticAIPlatform.OpenCORE
