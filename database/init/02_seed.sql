-- =====================================================================
-- Plantamoss 2026 - Datos semilla
-- =====================================================================
SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- Estados (los primeros 15 conservan los ids del proyecto original)
-- ---------------------------------------------------------------------
INSERT INTO estados (id_estado, nombre_estado, clima_predominante) VALUES
    (1,  'Jalisco',             'Semicálido subhúmedo'),
    (2,  'Hidalgo',             'Templado húmedo'),
    (3,  'Chiapas',             'Cálido húmedo'),
    (4,  'Ciudad de México',    'Templado subhúmedo'),
    (5,  'Nuevo León',          'Semiárido cálido'),
    (6,  'Tabasco',             'Cálido húmedo'),
    (7,  'Guerrero',            'Cálido subhúmedo'),
    (8,  'Yucatán',             'Cálido subhúmedo'),
    (9,  'Veracruz',            'Cálido húmedo'),
    (10, 'Puebla',              'Templado subhúmedo'),
    (11, 'Tlaxcala',            'Templado subhúmedo'),
    (12, 'Estado de México',    'Templado subhúmedo'),
    (13, 'Oaxaca',              'Cálido subhúmedo'),
    (14, 'Chihuahua',           'Semiárido templado'),
    (15, 'Campeche',            'Cálido subhúmedo'),
    (16, 'Aguascalientes',      'Semiárido templado'),
    (17, 'Baja California',     'Muy seco templado'),
    (18, 'Baja California Sur', 'Muy seco cálido'),
    (19, 'Coahuila',            'Seco semicálido'),
    (20, 'Colima',              'Cálido subhúmedo'),
    (21, 'Durango',             'Semiárido templado'),
    (22, 'Guanajuato',          'Semiárido templado'),
    (23, 'Michoacán',           'Templado subhúmedo'),
    (24, 'Morelos',             'Cálido subhúmedo'),
    (25, 'Nayarit',             'Cálido subhúmedo'),
    (26, 'Querétaro',           'Semiárido templado'),
    (27, 'Quintana Roo',        'Cálido subhúmedo'),
    (28, 'San Luis Potosí',     'Seco semicálido'),
    (29, 'Sinaloa',             'Cálido subhúmedo'),
    (30, 'Sonora',              'Muy seco cálido'),
    (31, 'Tamaulipas',          'Cálido subhúmedo'),
    (32, 'Zacatecas',           'Semiárido templado');

-- ---------------------------------------------------------------------
-- Catálogo de flora
-- ---------------------------------------------------------------------
INSERT INTO catalogo_flora
    (id_planta, nombre_planta, nombre_cientifico, letra_inicial, id_estado, url_imagen,
     tipo_clima, luz_sol, riego, descripcion, cuidados_texto)
VALUES
(1, 'Agave azul', 'Agave tequilana', 'A', 1,
 '/img/agave-azul.jpeg',
 'arido', 'sol_directo', 'Escaso, cada 2-3 semanas',
 'Suculenta de hojas largas y rígidas de color azul grisáceo, cultivada principalmente en los Altos y el valle de Tequila. De su piña cocida y fermentada se destila el tequila, bebida con denominación de origen.',
 E'Riego: escaso; deja secar el sustrato por completo entre riegos.\nLuz: sol directo durante la mayor parte del día.\nSuelo: arenoso o volcánico con excelente drenaje.\nDato: tarda de 6 a 8 años en estar lista para la jima.'),

(2, 'Biznaga', 'Echinocactus platyacanthus', 'B', 2,
 '/img/biznaga.jpeg',
 'arido', 'sol_directo', 'Muy escaso, cada 3-4 semanas',
 'Cactus globoso de gran tamaño con costillas marcadas y espinas robustas, endémico de las zonas semiáridas del centro de México. Su pulpa se usaba para elaborar el dulce de acitrón, hoy restringido por estar protegida.',
 E'Riego: muy escaso; suspéndelo en invierno.\nLuz: sol directo o luz muy intensa.\nSuelo: mezcla para cactáceas con grava y arena.\nDato: puede vivir más de 100 años y es especie sujeta a protección especial.'),

(3, 'Ceiba', 'Ceiba pentandra', 'C', 3,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/57/Panguana-Lupuna-2008-2.jpg/330px-Panguana-Lupuna-2008-2.jpg',
 'tropical', 'sol_directo', 'Moderado, 1-2 veces por semana',
 'Árbol monumental de las selvas tropicales, con tronco recto y grandes raíces tabulares. Para los mayas era el árbol sagrado que unía el inframundo, la tierra y el cielo.',
 E'Riego: moderado mientras es joven; ya establecido tolera la sequía.\nLuz: pleno sol.\nSuelo: profundo, fértil y bien drenado.\nDato: puede superar los 50 metros de altura y su fruto produce una fibra algodonosa llamada kapok.'),

(4, 'Cacao', 'Theobroma cacao', 'C', 6,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/5a/Matadecacao.jpg/330px-Matadecacao.jpg',
 'tropical', 'sombra_parcial', 'Frecuente, mantener húmedo',
 'Árbol pequeño de sotobosque cuyas mazorcas crecen directamente sobre el tronco. Sus semillas fermentadas y tostadas son la base del chocolate, bebida ceremonial de olmecas, mayas y mexicas.',
 E'Riego: frecuente; el sustrato debe mantenerse húmedo sin encharcarse.\nLuz: sombra parcial bajo árboles más altos.\nSuelo: rico en materia orgánica, ligeramente ácido.\nDato: los mexicas usaban sus semillas como moneda.'),

(5, 'Dalia', 'Dahlia coccinea', 'D', 4,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8c/Dahlia_coccinea_%28Asteraceae%29.jpg/330px-Dahlia_coccinea_%28Asteraceae%29.jpg',
 'templado', 'sol_directo', 'Moderado, 2-3 veces por semana',
 'Planta herbácea de raíces tuberosas y flores rojas o anaranjadas muy vistosas. La dalia fue declarada flor nacional de México en 1963.',
 E'Riego: moderado en temporada de floración, reducido en invierno.\nLuz: sol directo al menos 6 horas.\nSuelo: fértil, suelto y bien drenado.\nDato: los mexicas la llamaban acocoxóchitl y usaban sus tallos huecos para conducir agua.'),

(6, 'Ahuehuete', 'Taxodium mucronatum', 'A', 4,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f7/El_Sabinal%2C_Salto_de_los_Salado%2C_Aguascalientes_51.JPG/330px-El_Sabinal%2C_Salto_de_los_Salado%2C_Aguascalientes_51.JPG',
 'templado', 'sol_directo', 'Abundante, cerca de cuerpos de agua',
 'Conífera longeva que crece a orillas de ríos y manantiales, con follaje fino y colgante. Es el árbol nacional de México desde 1921 y el célebre Árbol del Tule es un ejemplar de esta especie.',
 E'Riego: abundante; prefiere suelos permanentemente húmedos.\nLuz: pleno sol.\nSuelo: profundo, aluvial y con buena humedad.\nDato: su nombre náhuatl significa "viejo del agua".'),

(7, 'Palma china', 'Yucca filifera', 'P', 5,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/9/9e/Yucca_filifera_JOT.jpg/330px-Yucca_filifera_JOT.jpg',
 'arido', 'sol_directo', 'Escaso, cada 3 semanas',
 'Yuca arborescente del noreste de México con hojas rígidas que desprenden filamentos en los bordes. En primavera produce grandes racimos colgantes de flores blancas comestibles.',
 E'Riego: escaso; muy tolerante a la sequía.\nLuz: sol directo.\nSuelo: pedregoso o arenoso, bien drenado.\nDato: crece muy lento, apenas unos centímetros por año.'),

(8, 'Anacahuita', 'Cordia boissieri', 'A', 5,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a9/Cordia_boisseri_whole.jpg/330px-Cordia_boisseri_whole.jpg',
 'arido', 'sol_directo', 'Moderado, semanal en verano',
 'Árbol pequeño del matorral tamaulipeco con hojas aterciopeladas y flores blancas de garganta amarilla. Es la flor emblemática de Nuevo León y se usa mucho en el arbolado urbano de Monterrey.',
 E'Riego: semanal en verano; ya establecido resiste la sequía.\nLuz: sol directo.\nSuelo: calizo o pedregoso con buen drenaje.\nDato: su fruto y hojas se han usado en remedios tradicionales contra la tos.'),

(9, 'Platanillo', 'Heliconia latispatha', 'P', 6,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/23/Heliconia_latispatha_%28Starwiz%29.jpg/330px-Heliconia_latispatha_%28Starwiz%29.jpg',
 'tropical', 'sombra_parcial', 'Frecuente, 3 veces por semana',
 'Herbácea tropical de hojas grandes parecidas a las del plátano e inflorescencias erectas de brácteas rojas y amarillas. Es visitada por colibríes, sus principales polinizadores.',
 E'Riego: frecuente; requiere humedad constante.\nLuz: sombra parcial o sol filtrado.\nSuelo: rico en humus y bien drenado.\nDato: sus brácteas acumulan agua que sirve de hábitat a pequeños insectos.'),

(10, 'Cuachalalate', 'Amphipterygium adstringens', 'C', 7,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e5/Amphipterygium_adstringens.jpg/330px-Amphipterygium_adstringens.jpg',
 'tropical', 'sol_directo', 'Escaso, solo en temporada seca prolongada',
 'Árbol de la selva baja caducifolia del Pacífico mexicano, de corteza gruesa y resinosa. Su corteza es una de las plantas medicinales más comercializadas del país, usada para problemas gástricos.',
 E'Riego: escaso; está adaptado a largas temporadas secas.\nLuz: sol directo.\nSuelo: pedregoso y bien drenado.\nDato: pierde todas sus hojas durante la época de secas.'),

(11, 'Nochebuena', 'Euphorbia pulcherrima', 'N', 7,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e1/Weihnachtsstern_-_gro%C3%9F.jpg/330px-Weihnachtsstern_-_gro%C3%9F.jpg',
 'tropical', 'sombra_parcial', 'Moderado, cuando la superficie esté seca',
 'Arbusto nativo de las barrancas de Guerrero cuyas brácteas rojas parecen pétalos. Los mexicas la llamaban cuetlaxóchitl y hoy es símbolo navideño en todo el mundo.',
 E'Riego: moderado; evita encharcar el sustrato.\nLuz: luz brillante indirecta o sombra parcial.\nSuelo: ligero, fértil y bien drenado.\nDato: necesita noches largas de oscuridad total para que sus brácteas se pinten de rojo.'),

(12, 'Henequén', 'Agave fourcroydes', 'H', 8,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/c2/Lanzarote_-_Agave_fourcroydes.jpg/330px-Lanzarote_-_Agave_fourcroydes.jpg',
 'arido', 'sol_directo', 'Escaso, cada 2-3 semanas',
 'Agave de hojas largas y grisáceas cultivado en la península de Yucatán por su fibra resistente. A finales del siglo XIX el llamado oro verde hizo de Mérida una de las ciudades más ricas de México.',
 E'Riego: escaso; tolera suelos muy secos.\nLuz: sol directo.\nSuelo: pedregoso y calizo, típico de Yucatán.\nDato: su fibra se usa para hacer cuerdas, costales y hamacas.'),

(13, 'Ramón', 'Brosimum alicastrum', 'R', 8,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/51/Brosimum-Alicastrum_02.jpg/330px-Brosimum-Alicastrum_02.jpg',
 'tropical', 'sol_directo', 'Moderado, semanal',
 'Árbol perennifolio de la selva maya con frutos pequeños y semillas muy nutritivas. Su semilla, conocida como ox en maya, se tuesta y muele para preparar harinas y bebidas similares al café.',
 E'Riego: moderado mientras se establece.\nLuz: sol directo o semisombra en etapa juvenil.\nSuelo: calizo y profundo.\nDato: sus hojas son forraje para el ganado en la temporada seca.'),

(14, 'Vainilla', 'Vanilla planifolia', 'V', 9,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e6/Vanilla_planifolia_-_K%C3%B6hler%E2%80%93s_Medizinal-Pflanzen-278.jpg/330px-Vanilla_planifolia_-_K%C3%B6hler%E2%80%93s_Medizinal-Pflanzen-278.jpg',
 'tropical', 'sombra_parcial', 'Frecuente, ambiente húmedo',
 'Orquídea trepadora originaria de la región totonaca de Papantla, Veracruz. Sus vainas curadas producen la vainilla, una de las especias más apreciadas del mundo.',
 E'Riego: frecuente; prefiere humedad ambiental alta.\nLuz: sombra parcial, con tutor o árbol de soporte.\nSuelo: hojarasca y materia orgánica muy aireada.\nDato: fuera de México su flor se poliniza a mano porque falta su abeja polinizadora.'),

(15, 'Costilla de Adán', 'Monstera deliciosa', 'C', 9,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/2e/Monstera_deliciosa2.jpg/330px-Monstera_deliciosa2.jpg',
 'tropical', 'sombra_parcial', 'Moderado, 1 vez por semana',
 'Trepadora de las selvas húmedas del sureste mexicano con grandes hojas perforadas. Es una de las plantas de interior más populares del mundo.',
 E'Riego: moderado; deja secar la capa superior del sustrato.\nLuz: luz indirecta brillante o sombra parcial.\nSuelo: sustrato aireado con corteza y materia orgánica.\nDato: su fruto maduro es comestible y sabe a una mezcla de piña y plátano.'),

(16, 'Cempasúchil', 'Tagetes erecta', 'C', 10,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/2f/Tagetes_x_erecta1.jpg/330px-Tagetes_x_erecta1.jpg',
 'templado', 'sol_directo', 'Moderado, 2-3 veces por semana',
 'Planta anual de flores anaranjadas y amarillas con aroma intenso, cultivada ampliamente en Puebla. Es la flor tradicional de las ofrendas de Día de Muertos, cuyo color guía a las almas.',
 E'Riego: moderado, sin mojar las flores.\nLuz: sol directo.\nSuelo: fértil y bien drenado.\nDato: su nombre viene del náhuatl cempōhualxōchitl, "veinte flores".'),

(17, 'Maguey pulquero', 'Agave salmiana', 'M', 11,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/0/08/Agave_salmiana_-_San_Francisco_Botanical_Garden_-_DSC09789.JPG/330px-Agave_salmiana_-_San_Francisco_Botanical_Garden_-_DSC09789.JPG',
 'arido', 'sol_directo', 'Escaso, cada 3 semanas',
 'Agave robusto de hojas anchas y carnosas típico del altiplano. De su corazón se extrae el aguamiel que, al fermentar, se convierte en pulque.',
 E'Riego: escaso; resiste heladas ligeras y sequía.\nLuz: sol directo.\nSuelo: pobre, pedregoso y con buen drenaje.\nDato: de sus pencas se obtiene el mixiote y sus quiotes se usan como material de construcción.'),

(18, 'Oyamel', 'Abies religiosa', 'O', 12,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/aa/Abies_religiosa%2C_Parque_Nacional_El_Chico%2C_Hidalgo%2C_Mexico_1.jpg/330px-Abies_religiosa%2C_Parque_Nacional_El_Chico%2C_Hidalgo%2C_Mexico_1.jpg',
 'frio', 'sol_directo', 'Moderado, humedad constante',
 'Abeto de alta montaña que forma bosques entre los 2,500 y 3,600 metros de altitud. Sus bosques en el Estado de México y Michoacán son el refugio invernal de la mariposa monarca.',
 E'Riego: moderado y constante; no tolera la sequía prolongada.\nLuz: sol directo o semisombra en etapa joven.\nSuelo: volcánico, ácido y rico en materia orgánica.\nDato: sus ramas se usaban en ceremonias religiosas, de ahí su nombre científico.'),

(19, 'Agave espadín', 'Agave angustifolia', 'A', 13,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/0/03/Agave_angustifolia-_plants_near_Yagul_Oaxaca.jpg/330px-Agave_angustifolia-_plants_near_Yagul_Oaxaca.jpg',
 'arido', 'sol_directo', 'Escaso, cada 2-3 semanas',
 'Agave de hojas angostas y verdes, el más cultivado en los valles centrales de Oaxaca. Con él se elabora la mayor parte del mezcal que se produce en México.',
 E'Riego: escaso; deja secar el suelo entre riegos.\nLuz: sol directo.\nSuelo: arenoso o pedregoso con buen drenaje.\nDato: madura en unos 7 a 8 años y genera muchos hijuelos para su propagación.'),

(20, 'Cazahuate', 'Ipomoea arborescens', 'C', 13,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/1/1a/Casahuate_Mt_Alban_Mex.jpg/330px-Casahuate_Mt_Alban_Mex.jpg',
 'tropical', 'sol_directo', 'Escaso, solo en sequía prolongada',
 'Árbol de la selva baja con tronco blanquecino que florece en la temporada seca con grandes flores blancas en forma de embudo. Es pariente arbóreo de las enredaderas conocidas como manto de la virgen.',
 E'Riego: escaso; está adaptado a la temporada seca.\nLuz: sol directo.\nSuelo: pedregoso, calizo o volcánico.\nDato: sobre su tronco crece un hongo comestible conocido como oreja de cazahuate.'),

(21, 'Sotol', 'Dasylirion wheeleri', 'S', 14,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/55/Dasylirion_wheeleri_1.jpg/330px-Dasylirion_wheeleri_1.jpg',
 'arido', 'sol_directo', 'Muy escaso, cada 4 semanas',
 'Planta del desierto chihuahuense con una roseta de hojas delgadas, azuladas y con espinas en el borde. De su corazón cocido se destila el sotol, bebida con denominación de origen.',
 E'Riego: muy escaso; extremadamente tolerante a la sequía.\nLuz: sol directo.\nSuelo: rocoso o arenoso.\nDato: soporta heladas y temperaturas extremas del desierto.'),

(22, 'Palo de tinte', 'Haematoxylum campechianum', 'P', 15,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a1/Haematoxylon_campechianum0.jpg/330px-Haematoxylon_campechianum0.jpg',
 'tropical', 'sol_directo', 'Moderado, semanal',
 'Árbol espinoso de tronco acanalado que crece en zonas inundables de la península de Yucatán. Su madera produce un tinte rojo-violeta que fue exportado masivamente a Europa durante la Colonia.',
 E'Riego: moderado; tolera suelos inundables temporalmente.\nLuz: sol directo.\nSuelo: arcilloso o calizo.\nDato: de él se obtiene la hematoxilina, un colorante usado en laboratorios de histología.'),

(23, 'Sahuaro', 'Carnegiea gigantea', 'S', 30,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/c/cd/Carnegiea_gigantea_in_Saguaro_National_Park_near_Tucson%2C_Arizona_during_November_%2858%29.jpg/330px-Carnegiea_gigantea_in_Saguaro_National_Park_near_Tucson%2C_Arizona_during_November_%2858%29.jpg',
 'arido', 'sol_directo', 'Muy escaso, mensual',
 'Cactus columnar gigante emblemático del desierto de Sonora, con brazos que aparecen después de décadas. Sus flores blancas se abren de noche y son polinizadas por murciélagos.',
 E'Riego: muy escaso; casi nulo en invierno.\nLuz: sol directo intenso.\nSuelo: arenoso y pedregoso con drenaje perfecto.\nDato: puede vivir más de 150 años y superar los 12 metros de altura.'),

(24, 'Cardón', 'Pachycereus pringlei', 'C', 18,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/5/5d/Pachycereus_pringlei_%285782057703%29.jpg/330px-Pachycereus_pringlei_%285782057703%29.jpg',
 'arido', 'sol_directo', 'Muy escaso, mensual',
 'Considerado el cactus más alto del mundo, domina los paisajes de Baja California Sur. Su tronco masivo almacena grandes cantidades de agua para sobrevivir largas sequías.',
 E'Riego: muy escaso; solo en verano si no llueve.\nLuz: sol directo.\nSuelo: arenoso o rocoso, muy bien drenado.\nDato: puede alcanzar cerca de 20 metros de altura.'),

(25, 'Aguacate', 'Persea americana', 'A', 23,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f2/Persea_americana_fruit_2.JPG/330px-Persea_americana_fruit_2.JPG',
 'templado', 'sol_directo', 'Moderado, 1-2 veces por semana',
 'Árbol perennifolio domesticado en Mesoamérica hace miles de años, cuyo fruto es base del guacamole. Michoacán es el principal productor de aguacate del mundo.',
 E'Riego: moderado y profundo; evita el encharcamiento.\nLuz: sol directo.\nSuelo: profundo, suelto y bien drenado.\nDato: su nombre proviene del náhuatl ahuacatl.'),

(26, 'Palma chit', 'Thrinax radiata', 'P', 27,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/7/76/Thrinax_Radiata.JPG/330px-Thrinax_Radiata.JPG',
 'tropical', 'sol_directo', 'Moderado, semanal',
 'Palma de abanico de tronco delgado que crece en las costas y dunas del Caribe mexicano. Sus hojas se han usado tradicionalmente para techar palapas.',
 E'Riego: moderado; tolera la sal y la sequía una vez establecida.\nLuz: sol directo.\nSuelo: arenoso o calizo.\nDato: es una especie amenazada por la sobreexplotación de sus hojas y el desarrollo costero.'),

(27, 'Garambullo', 'Myrtillocactus geometrizans', 'G', 26,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/8/8b/Blue_Candle_Cactus_%28Myrtillocactus_geometrizans%29_%2835773208785%29.jpg/330px-Blue_Candle_Cactus_%28Myrtillocactus_geometrizans%29_%2835773208785%29.jpg',
 'arido', 'sol_directo', 'Escaso, cada 3 semanas',
 'Cactus columnar ramificado de color verde azulado, común en el semidesierto queretano. Produce pequeños frutos morados y dulces que se comen frescos o en nieves.',
 E'Riego: escaso; nulo en invierno.\nLuz: sol directo.\nSuelo: mezcla para cactáceas con buen drenaje.\nDato: sus frutos, llamados garambullos, son ricos en antioxidantes.'),

(28, 'Chamal', 'Dioon edule', 'C', 28,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e9/Dioon_edule-IMG_9697.JPG/330px-Dioon_edule-IMG_9697.JPG',
 'tropical', 'sombra_parcial', 'Escaso, cada 2 semanas',
 'Cícada endémica de México con hojas rígidas parecidas a las de una palma, presente en la Huasteca potosina. Pertenece a un linaje de plantas que existía desde la era de los dinosaurios.',
 E'Riego: escaso; no tolera el exceso de agua.\nLuz: sol filtrado o sombra parcial.\nSuelo: pedregoso y bien drenado.\nDato: algunos ejemplares superan los 1,000 años de edad.'),

(29, 'Candelilla', 'Euphorbia antisyphilitica', 'C', 19,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/6/61/Euphorbia_antisyphilitica2_ies.jpg/330px-Euphorbia_antisyphilitica2_ies.jpg',
 'arido', 'sol_directo', 'Muy escaso, cada 4 semanas',
 'Planta de tallos delgados y cerosos sin hojas aparentes que crece en el desierto de Coahuila. De ella se extrae la cera de candelilla, usada en cosméticos, chicles y velas.',
 E'Riego: muy escaso.\nLuz: sol directo.\nSuelo: calizo y pedregoso.\nDato: la cera la protege de la evaporación en el clima extremo del desierto.'),

(30, 'Nopal', 'Opuntia ficus-indica', 'N', 32,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/d/d4/Dikenli_%C4%B0ncir_%28Opuntia_ficus-indica%29_Gaziantep_Turkey.IMG_1104.jpg/330px-Dikenli_%C4%B0ncir_%28Opuntia_ficus-indica%29_Gaziantep_Turkey.IMG_1104.jpg',
 'arido', 'sol_directo', 'Escaso, cada 2-3 semanas',
 'Cactácea de tallos aplanados llamados pencas, presente en el escudo nacional. Zacatecas es uno de los principales productores de tuna, su fruto dulce y refrescante.',
 E'Riego: escaso; muy resistente a la sequía.\nLuz: sol directo.\nSuelo: pobre y bien drenado.\nDato: sus nopalitos tiernos se cocinan y son ricos en fibra.'),

(31, 'Cacaloxóchitl', 'Plumeria rubra', 'C', 24,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/aa/Champa_tree_with_pink_flowers_in_Islamabad%2C_Pakistan.jpg/330px-Champa_tree_with_pink_flowers_in_Islamabad%2C_Pakistan.jpg',
 'tropical', 'sol_directo', 'Moderado, semanal en verano',
 'Árbol pequeño de ramas gruesas y flores muy perfumadas en tonos rosas, blancos o amarillos. En Morelos se le conoce también como flor de mayo y se usa en ofrendas y collares.',
 E'Riego: moderado en verano, casi nulo en invierno.\nLuz: sol directo.\nSuelo: arenoso y bien drenado.\nDato: su nombre náhuatl significa "flor del cuervo".');

-- ---------------------------------------------------------------------
-- Usuarios semilla (correo verificado, bcrypt costo 10)
-- Contraseñas de prueba documentadas en database/README.md
-- ---------------------------------------------------------------------
INSERT INTO users (id, name, email, email_verified_at, password, role, estado) VALUES
    (1, 'Administrador Plantamoss', 'admin@plantamoss.com',    now(), crypt('Admin123!',    gen_salt('bf', 10)), 'admin',    'activo'),
    (2, 'Editor de Catálogo',       'editor@plantamoss.com',   now(), crypt('Editor123!',   gen_salt('bf', 10)), 'editor',   'activo'),
    (3, 'Editora de Noticias',      'noticias@plantamoss.com', now(), crypt('Noticias123!', gen_salt('bf', 10)), 'noticias', 'activo'),
    (4, 'Usuario de Prueba',        'usuario@plantamoss.com',  now(), crypt('Usuario123!',  gen_salt('bf', 10)), 'user',     'activo');

-- ---------------------------------------------------------------------
-- Noticias (id_editor = usuario noticias)
-- ---------------------------------------------------------------------
INSERT INTO noticias (id_noticia, titulo, contenido, url_imagen, url_externa, destacada, activa, id_editor, fecha_publicacion) VALUES
(1, 'Temporada de cempasúchil: millones de flores para Día de Muertos',
 'Productores de Puebla, Estado de México y Michoacán inician la cosecha de cempasúchil para las ofrendas de noviembre. Esta flor nativa de México no solo tiene valor cultural: sus pétalos se usan como colorante natural y sus compuestos tienen propiedades antiinflamatorias. Comprar plantas en maceta de productores locales ayuda a conservar las variedades criollas.',
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/2f/Tagetes_x_erecta1.jpg/330px-Tagetes_x_erecta1.jpg',
 NULL, true, true, 3, now() - interval '1 day'),
(2, 'La mariposa monarca regresa a los bosques de oyamel',
 'Las primeras colonias de mariposa monarca comienzan a llegar a los santuarios del Estado de México y Michoacán, donde pasarán el invierno colgadas de los oyameles. La conservación de estos bosques de alta montaña es clave para la migración: un solo árbol puede albergar miles de mariposas.',
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/a/aa/Abies_religiosa%2C_Parque_Nacional_El_Chico%2C_Hidalgo%2C_Mexico_1.jpg/330px-Abies_religiosa%2C_Parque_Nacional_El_Chico%2C_Hidalgo%2C_Mexico_1.jpg',
 NULL, false, true, 3, now() - interval '3 days'),
(3, 'La biznaga gigante, una especie que necesita protección',
 'La biznaga de dulce (Echinocactus platyacanthus) está clasificada como especie sujeta a protección especial por la NOM-059-SEMARNAT. Su extracción ilegal para elaborar acitrón y su lento crecimiento la hacen vulnerable. Si quieres tener cactáceas en casa, adquiérelas solo en viveros autorizados.',
 '/img/biznaga.jpeg',
 NULL, false, true, 3, now() - interval '7 days'),
(4, 'El ahuehuete, árbol nacional, vuelve a Paseo de la Reforma',
 'Un ahuehuete joven ocupa ahora la emblemática glorieta de Paseo de la Reforma en la Ciudad de México. Esta conífera, que puede vivir más de dos mil años, necesita suelos con mucha humedad, por lo que su cuidado en la ciudad requiere riego constante y monitoreo de especialistas.',
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/f/f7/El_Sabinal%2C_Salto_de_los_Salado%2C_Aguascalientes_51.JPG/330px-El_Sabinal%2C_Salto_de_los_Salado%2C_Aguascalientes_51.JPG',
 NULL, false, true, 3, now() - interval '12 days');

-- ---------------------------------------------------------------------
-- Miniblogs
-- ---------------------------------------------------------------------
INSERT INTO miniblogs (id_blog, id_usuario, titulo, contenido, contenido2, contenido3,
                       url_imagen, url_imagen_2, url_imagen_3, url_imagen_4, url_imagen_5, fecha_publicacion) VALUES
(1, 4, 'Mi primer huerto de suculentas mexicanas',
 'Empecé con un agave pequeño y una biznaga que compré en un vivero autorizado. Lo más difícil fue aprender a no regarlas de más: en casa las riego solo cuando el sustrato está completamente seco.',
 'Usé una mezcla de tierra para cactus, tezontle y un poco de arena de río. Las macetas de barro ayudan mucho porque dejan respirar las raíces.',
 'Después de seis meses ya salieron los primeros hijuelos del agave. ¡Paciencia y mucho sol!',
 '/img/agave-azul.jpeg',
 '/img/biznaga.jpeg',
 NULL, NULL, NULL, now() - interval '2 days'),
(2, 2, 'Plantas nativas para un balcón con sombra',
 'No todas las plantas mexicanas necesitan sol directo. La costilla de Adán y el platanillo crecen muy bien con luz filtrada, siempre que mantengas la humedad.',
 'Un truco que me funciona es agrupar las macetas para crear un microclima más húmedo y rociar las hojas en los días calurosos.',
 NULL,
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/2e/Monstera_deliciosa2.jpg/330px-Monstera_deliciosa2.jpg',
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/2/23/Heliconia_latispatha_%28Starwiz%29.jpg/330px-Heliconia_latispatha_%28Starwiz%29.jpg',
 NULL, NULL, NULL, now() - interval '5 days'),
(3, 1, 'Cómo cuidar tu nochebuena después de diciembre',
 'La nochebuena no es una planta de temporada: es un arbusto perenne originario de Guerrero que puede vivir muchos años si la cuidas bien.',
 'Al terminar la floración, poda los tallos a unos 15 cm, reduce el riego y colócala en un lugar con luz indirecta abundante.',
 'A partir de octubre dale 14 horas diarias de oscuridad total durante ocho semanas y sus brácteas volverán a ponerse rojas para Navidad.',
 'https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e1/Weihnachtsstern_-_gro%C3%9F.jpg/330px-Weihnachtsstern_-_gro%C3%9F.jpg',
 NULL, NULL, NULL, NULL, now() - interval '9 days');

-- ---------------------------------------------------------------------
-- Ajuste de secuencias
-- ---------------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('users', 'id'),                   COALESCE((SELECT max(id) FROM users), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('estados', 'id_estado'),          COALESCE((SELECT max(id_estado) FROM estados), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('catalogo_flora', 'id_planta'),   COALESCE((SELECT max(id_planta) FROM catalogo_flora), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('noticias', 'id_noticia'),        COALESCE((SELECT max(id_noticia) FROM noticias), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('miniblogs', 'id_blog'),          COALESCE((SELECT max(id_blog) FROM miniblogs), 0) + 1, false);
SELECT setval(pg_get_serial_sequence('password_reset_codes', 'id'),    COALESCE((SELECT max(id) FROM password_reset_codes), 0) + 1, false);

COMMIT;
