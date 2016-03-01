#	DataSync

Uddannelse: 	Datamatikeruddannelsen v/ Erhvervsakadami Aarhus

Niveauangivelse:	Hovedopgave

Synops:
DataSync er et automatiseret system, der kobler VTiger og E-conomic sammen, så man med lethed kan anvende VTiger som et salgsorgan i forlængelse af E-conomic bogholderi. DataSync henvender sig primært til virksomheder som har en salgsafdeling, der benytter sig af VTiger og et bogholderi, der benytter sig af E-conomic. Her er DataSync løsningen, der effektiviserer arbejdet ved at synkronisere de to systemers produkter og kunder, så de stemmer overens. Derudover kan en sælger, der har succes med sit salg, sende det direkte til fakturering og hurtigt komme videre med næste salg uden at spilde tid på manuel overlevering. DataSync fungerer uden installering af software og er ekstremt brugervenligt.

Programmel:
DataSync er et autonomt system, som bliver understøttet af en administration og et webside for kunderne, som alle benytter den samme database. Applikationerne er uafhængige af hinanden og kan  erstattes med andre teknologier, hvis det på et senere tidspunkt skulle vise at være nødvendigt. Det er  udviklet tre selvstændige Visual Studio projekter. 
Visual Studio projekter:
•	DataSync_Administration	(Administration)
•	DataSync_Engine	(Engine)
•	DataSync_Website	(Website)
Det kan ikke undgås, at der opstår duplikat kode i Administration og Engine, men det samlede system er designet således, da et uafhængighedskravet mellem modulerne er vægtet tungere end duplikering. Der er selvfølgelig taget højde for dette i programkoden. Website er helt kildekode-uafhængig af Administration og Website, da den udelukkende henter via deres fælles database. 

DataSync er lavet i programmeringssproget i C#/.Net på Visual Studio platformen. Integration til E-conomic foregår via .Net API, og Umbraco endvidere er udviklet i Visual Studio. VTiger-strategien er uafhængigt af programmeringssprog, da tilgangen til denne foregår via et REST API - POST, GET etc. metoder. Databaserne er MsSQL.
