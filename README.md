# Military Task App

**Military Task App** este o aplicație web concepută pentru a gestiona proiecte și sarcini într-un mod eficient, oferind funcționalități personalizate pentru diferite tipuri de utilizatori: utilizatori neînregistrați, membri, organizatori și administratori.

---

## **Funcționalități**

### **1. Tipuri de utilizatori**
- **Utilizator neînregistrat**:
  - Poate accesa pagina de prezentare a platformei.
  - Are acces la formularele de autentificare și înregistrare.

- **Membru**:
  - Poate comenta pe task-urile proiectelor din care face parte.
  - Are acces doar la task-urile și proiectele proprii.
  - Poate modifica sau șterge propriile comentarii.

- **Organizator**:
  - Devine automat organizator după crearea unui proiect.
  - Poate adăuga membri în proiect.
  - Creează, modifică, șterge și atribuie task-uri membrilor proiectului.

- **Administrator**:
  - Are acces complet la toate funcționalitățile aplicației.
  - Poate șterge task-uri, comentarii, echipe, utilizatori și poate modifica drepturile utilizatorilor.

---

### **2. Funcționalități cheie**
- **Pagina de prezentare**: Include informații utile despre aplicație și funcționalitățile sale, accesibilă utilizatorilor neînregistrați.
- **Proiecte**:
  - Fiecare proiect are o pagină dedicată în care sunt afișate task-urile asociate.
  - Organizatorii pot adăuga membri în proiect.
- **Task-uri**:
  - Fiecare task conține: titlu, descriere, status (Not Started, In Progress, Completed), data de început, data de finalizare și conținut media (text, imagini, videoclipuri embeded).
  - Data de finalizare trebuie să fie ulterioară datei de început.
  - Organizatorii pot crea, edita, șterge și atribui task-uri membrilor.
  - Membrii și organizatorii pot actualiza statusul unui task.
- **Comentarii**:
  - Membrii pot adăuga, edita și șterge propriile comentarii la task-urile proiectelor din care fac parte.
- **Administrare**:
  - Administratorii gestionează utilizatorii, proiectele și task-urile, asigurând buna funcționare a aplicației.
  - Administratorii pot activa sau revoca drepturile utilizatorilor
    
### Cerinte

- Sa existe 4 tipuri de utilizatori: utilizator neinregistrat, organizator (nu
trebuie neaparat sa fie rol – poate fi implementat si din logica de backend),
membru (este un user inregistrat) si administrator (0.5p).  DONE

- Utilizatorii neinregistrati pot sa vada pagina de prezentare a platformei si
formularele de autentificare si inregistrare. Pagina de prezentare a platformei
se realizeaza cu elemente pe care echipa le considera utile si pe care doreste
sa le prezinte catre toti utilizatorii care acceseaza platforma (0.5p). Daca
aceasta nu contine alte elemente in afara de login si register, nu se acorda
intregul punctaj.  DONE 

- Utilizatorii care creeaza un proiect devin automat organizatori.
Organizatorul, avand acest rol punctual, nu trebuie sa fie in mod obligatoriu
un rol in aplicatie. Se poate implementa aceasta functionalitate in logica de
backend. (0.5p).  DONE

- Organizatorii pot adauga membri si pot crea task-uri (1.0p). (doar taskuri deocamdata)
  
- Task-urile contin titlu, descriere, status, data start, data finalizare si continut
media: text, imagini sau videoclip-uri de pe alte platforme (embedded).
Toate campurile sunt obligatorii. Data de finalizare trebuie sa fie mai mare
decat data de inceput. (1.0p).  DONE

- Organizatorii adauga, modifica si sterg task-uri si asigneaza task-urile
membrilor proiectului (1.0p).  DONE

- Membrii pot doar sa lase comentarii la task-urile existente, doar in proiectele
din care fac parte, si sa isi modifice sau sa isi stearga comentariile proprii
(1.0p).  DONE

- Membrii au access doar la task-urile alocate proiectelor din care acestia fac
parte (0.5p).  DONE

- Atat organizatorul, cat si membrii, pot modifica statusul unui task: not
started, in progress, completed (1.0p).  DONE

- Fiecare proiect are task-urile listate in pagina speciala a proiectului (0.5p).  DONE

- Administratorul are acces la tot ce cuprinde aplicatia si se ocupa de buna
functionare a acesteia. Acesta poate sterge task-uri, comentarii, echipe,
utilizatori, etc, si poate activa si revoca drepturile utilizatorilor. (1.5p).  DONE
