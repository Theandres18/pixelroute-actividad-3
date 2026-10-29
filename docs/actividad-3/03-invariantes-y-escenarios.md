# 03. Invariantes y escenarios de aceptación

## Invariantes
Una **invariante** es una regla que siempre debe cumplirse. Las del agregado `Delivery` son:

| ID | Invariante | Dónde se protege | Prueba |
|---|---|---|---|
| INV-01 | Una entrega tiene **como máximo un repartidor activo**. | `Delivery.AssignCourier` | E-02 |
| INV-02 | **Solo una entrega Pendiente** puede asignarse. | `Delivery.AssignCourier` | E-07 |
| INV-03 | Una entrega Pendiente **no tiene** repartidor; una Asignada **siempre lo tiene** (estado y repartidor nunca se contradicen). | `Delivery` (setters privados) | E-01, E-09 |
| INV-04 | **No se asigna** a un repartidor fuera de línea. | `Delivery.AssignCourier` | E-03 |
| INV-05 | **No se asigna** a un repartidor con la ruta saturada (entregas activas ≥ máximo). | `Delivery.AssignCourier` | E-04 |
| INV-06 | Una entrega **Entregada o Cancelada** no cambia de repartidor ni de estado. Solo se cancela desde Pendiente o Asignada. | `Delivery.Cancel`, `AssignCourier` | E-07 |
| INV-07 | La **dirección** de entrega es obligatoria. | `DeliveryAddress` | E-10 |
| INV-08 | La **promesa** de entrega se calcula al crear la entrega y **no cambia** al asignar o reasignar. | `DeliveryPromise`, `Delivery` | `La_promesa_no_cambia...` |

Las invariantes viven **en el dominio**, no en el caso de uso ni en la base de datos. Así se cumplen sin importar quién invoque la operación (una API, una pantalla, un proceso automático o una prueba).

> **Nota sobre concurrencia:** si dos despachadores asignan la misma entrega al mismo tiempo, INV-01 también necesita una protección al guardar (control de versión optimista). Eso depende de la persistencia real y queda para la Actividad 4. Ver [ADR-004](adr/ADR-004-puertos-eventos-y-consistencia.md).

## Escenarios de aceptación (Dado-Cuando-Entonces)
Siguen la idea del video 5 (BDD): expresar el comportamiento en frases que entiende el negocio y que se pueden verificar.

### E-01: Asignación exitosa
- **Dado** una entrega Pendiente y un repartidor en línea con 1 entrega activa de un máximo de 3
- **Cuando** el despachador asigna al repartidor a la entrega
- **Entonces** la entrega queda Asignada a ese repartidor, con la hora de asignación registrada

### E-02: La entrega ya tiene repartidor
- **Dado** una entrega que ya está Asignada al repartidor A
- **Cuando** el despachador intenta asignarla al repartidor B
- **Entonces** el sistema rechaza la operación y la entrega sigue asignada al repartidor A

### E-03: Repartidor fuera de línea
- **Dado** una entrega Pendiente y un repartidor fuera de línea
- **Cuando** el despachador intenta asignarla a ese repartidor
- **Entonces** el sistema rechaza la operación y la entrega sigue Pendiente y sin repartidor

### E-04: Ruta saturada
- **Dado** una entrega Pendiente y un repartidor con 3 entregas activas de un máximo de 3
- **Cuando** el despachador intenta asignarla a ese repartidor
- **Entonces** el sistema rechaza la operación, no guarda cambios y no publica ningún evento

### E-05: Entrega inexistente
- **Dado** que no existe una entrega con el identificador indicado
- **Cuando** el despachador intenta asignar un repartidor a esa entrega
- **Entonces** el sistema informa que la entrega no existe y no guarda ni publica nada

### E-06: Repartidor inexistente
- **Dado** una entrega Pendiente y un repartidor que no existe
- **Cuando** el despachador intenta asignarlo
- **Entonces** el sistema informa que el repartidor no existe y no guarda ni publica nada

### E-07: Entrega cancelada
- **Dado** una entrega Cancelada
- **Cuando** el despachador intenta asignarle un repartidor
- **Entonces** el sistema rechaza la operación porque la entrega no está Pendiente

### E-08: Se publica el hecho de la asignación
- **Dado** una asignación exitosa
- **Cuando** el sistema termina de guardar
- **Entonces** se publica el evento "entrega asignada" con la entrega, el repartidor y la hora, para avisar al cliente

### E-09: Liberar y reasignar
- **Dado** una entrega Asignada al repartidor A
- **Cuando** el repartidor A es liberado y se asigna al repartidor B
- **Entonces** la entrega queda Pendiente tras liberar y luego Asignada al repartidor B

### E-10: Dirección vacía
- **Dado** que se intenta crear una entrega con la dirección vacía
- **Cuando** se construye la dirección de entrega
- **Entonces** el sistema la rechaza

### E-11: Un fallo no deja cambios a medias
- **Dado** una asignación que viola una regla (por ejemplo, ruta saturada)
- **Cuando** el sistema procesa la solicitud
- **Entonces** no se guarda ningún cambio y no se publica ningún evento

## Trazabilidad: escenarios, pruebas y requisitos
| Escenario | Prueba automática | Invariante | Requisito / escenario de calidad |
|---|---|---|---|
| E-01 | `DeliveryTests.AssignCourier_en_entrega_pendiente...`, `AssignCourierUseCaseTests.Asigna_guarda_y_publica...`, `AssignCourierFlowTests` | INV-03 | RF-10 |
| E-02 | `DeliveryTests.AssignCourier_dos_veces...`, `AssignCourierUseCaseTests.Entrega_ya_asignada...` | INV-01 | RNF-01 |
| E-03 | `DeliveryTests.AssignCourier_con_repartidor_fuera_de_linea...` | INV-04 | RF-10 |
| E-04 y E-11 | `DeliveryTests.AssignCourier_con_ruta_saturada...`, `AssignCourierUseCaseTests.Ruta_saturada...` | INV-05 | RF-10, R10 |
| E-05 | `AssignCourierUseCaseTests.Entrega_inexistente...` | n/a | RF-10 |
| E-06 | `AssignCourierUseCaseTests.Repartidor_inexistente...` | n/a | RF-10 |
| E-07 | `DeliveryTests.AssignCourier_en_entrega_cancelada...` | INV-02, INV-06 | RF-10 |
| E-08 | `DeliveryTests.AssignCourier_registra_el_evento...`, `AssignCourierUseCaseTests.Asigna_guarda_y_publica...` | n/a | RF-18, RNF-08 |
| E-09 | `DeliveryTests.ReleaseCourier_devuelve...` | INV-03 | RF-17 |
| E-10 | `DeliveryTests.DeliveryAddress_vacia...` | INV-07 | RF-01 |
| n/a | `DeliveryTests.La_promesa_no_cambia...` | INV-08 | RF-09 |
| n/a | `DependencyRulesTests` | Dirección de dependencias | RNF-10, QS-15, CV-01 |
