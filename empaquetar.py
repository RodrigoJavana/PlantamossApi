"""Empaqueta la carpeta publish/ en publish.zip con rutas estilo Linux ('/').

Compress-Archive de Windows guarda las rutas con '\\' y Azure App Service (Linux) falla
al descomprimir. Uso:  python empaquetar.py
"""
import os
import zipfile

BASE = 'publish'
SALIDA = 'publish.zip'

with zipfile.ZipFile(SALIDA, 'w', zipfile.ZIP_DEFLATED) as z:
    for raiz, _, archivos in os.walk(BASE):
        for nombre in archivos:
            ruta = os.path.join(raiz, nombre)
            z.write(ruta, os.path.relpath(ruta, BASE).replace(os.sep, '/'))

nombres = zipfile.ZipFile(SALIDA).namelist()
print('entradas:', len(nombres))
print('con backslash:', sum(chr(92) in n for n in nombres))
print('PlantamossApi.dll en la raiz:', 'PlantamossApi.dll' in nombres)
print('secretos:', [n for n in nombres if n.startswith('.env') or 'Development' in n])
