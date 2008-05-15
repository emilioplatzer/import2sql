/*
 * Creado por SharpDevelop.
 * Usuario: ipcsl2
 * Fecha: 15/05/2008
 * Hora: 9:37
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;
using Indices;

namespace Tareas
{
	public class ParametrosSegundoProc:Parametros{
		public string NombreBase;
		public string NombreBaseImportacion;
		public string NombreBaseEnBlanco;
		public ParametrosSegundoProc(LeerPorDefecto queHacer,string N):base(queHacer,N){}	
	}
	public class SegundoProc{
		ParametrosPruebasExternas param;
		BdAccess db;
		RepositorioIndice repo;
		public SegundoProc(){
			param=new ParametrosPruebasExternas(Parametros.LeerPorDefecto.SI,"repo2");
		}
		public void AbrirBase(){
			db=BdAccess.Abrir(param.NombreBase);
			repo=new RepositorioIndice(db);
		}
		public void ArmarBase(){
			System.Console.WriteLine("Recreando la base");
			Archivo.Borrar(param.NombreBase);
			Archivo.Copiar(param.NombreBaseEnBlanco,param.NombreBase);
			AbrirBase();
			repo.CrearTablas();
			System.Console.WriteLine("Importando codigos");
			db.EjecutrarSecuencia(
				@"
				create table PreciosImportados(
				Ano integer,
				Mes integer,
				Informante integer,
				producto varchar(8),
				Nombre varchar(100),
				Especificacion integer,
				Variedad integer,
				Precio double precision,
				Origen varchar(250)); "+
				@"INSERT INTO tipoinf (tipoinformante,otrotipoinformante) VALUES ('S','T');"+
				@"INSERT INTO tipoinf (tipoinformante,otrotipoinformante) VALUES ('T','S');"+
				@"INSERT INTO productos (producto,nombreproducto) "+
				@" SELECT 'P' & producto,nombreproducto FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO especificaciones (producto,especificacion,nombreespecificacion,tamannonormal) SELECT 'P' & producto,1,especificacion,null FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO variedades (producto,especificacion,variedad,nombrevariedad,tamanno,unidad) SELECT 'P' & producto,1,1,null,null,null FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"UPDATE variedades SET codigovariedad=producto & iif(especificacion>1,'.' & especificacion,'') & iif(variedad>1,'/' & variedad,'');"+
				@"INSERT INTO agrupaciones (agrupacion) VALUES ('A');"+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) VALUES ('A','A','Nivel General',null,1,0,'N'); "+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) "+
				@"SELECT 'A','A' & Grupo,NombreGrupo,'A' & left(Grupo,nivel-1),ponderador,nivel,'N' "+
				@" FROM Grupos IN '"+param.NombreBaseImportacion+"' WHERE nivel<6 AND nivel>0 ORDER BY nivel,Grupo;"+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) "+
				@"SELECT 'A','P' & producto,null,'A' & left(producto,5),ponderador,6,'S' "+
				@" FROM Productos IN '"+param.NombreBaseImportacion+"';"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"INSERT INTO informantes (informante,nombreInformante,tipoinformante) "+
				@" SELECT informante,nombreInformante,iif(tipoinformante='S' or tipoinformante='H','S','T') FROM informantes IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO preciosImportados (ano,mes,informante,producto,especificacion,variedad,precio,origen) "+
				@" SELECT ano,mes,informante,'P' & producto,obs,1,precio,origen FROM PreciosImportados IN '"+param.NombreBaseImportacion+"';"+
				@"CREATE VIEW [Control Codigos Informante] AS
					SELECT PreciosImportados.informante, PreciosImportados.Origen
					FROM PreciosImportados LEFT JOIN informantes ON PreciosImportados.informante = informantes.informante
					WHERE (((informantes.informante) Is Null))
					GROUP BY PreciosImportados.informante, PreciosImportados.Origen;"+
				@"CREATE VIEW [Control Codigos Producto] AS
					SELECT PreciosImportados.producto, PreciosImportados.Nombre, PreciosImportados.Especificacion, PreciosImportados.Origen
					FROM PreciosImportados LEFT JOIN especificaciones ON PreciosImportados.producto = especificaciones.producto AND preciosImportados.especificacion=especificaciones.especificacion
					WHERE especificaciones.especificacion Is Null
					GROUP BY PreciosImportados.producto, PreciosImportados.Nombre, PreciosImportados.Especificacion, PreciosImportados.Origen;"+
				@"INSERT INTO especificaciones (producto,especificacion,nombreespecificacion,tamannonormal) SELECT 'P' & producto,obs,'P' & producto & ' obs ' & obs,null FROM preciosimportados IN '"+param.NombreBaseImportacion+"' WHERE obs>1 GROUP BY producto,obs;"+
				@"CREATE TABLE Opciones (producto varchar(8), foreign key (producto) references productos (producto))"
				/*
				@"CREATE VIEW Matriz_RelVar AS TRANSFORM Avg(relvar.precio) AS PromedioDeprecio
					SELECT relvar.producto, relvar.especificacion, relvar.variedad, relvar.informante
					FROM relvar INNER JOIN Opciones ON relvar.producto = Opciones.producto
					GROUP BY relvar.producto, relvar.especificacion, relvar.variedad, relvar.informante
					PIVOT relvar.periodo;"+
				@"CREATE VIEW Matriz_CalEspInf AS TRANSFORM Avg(calespinf.promedioespinf) AS PromedioDepromedioespinf
					SELECT calespinf.producto, calespinf.especificacion, calespinf.informante
					FROM Opciones INNER JOIN calespinf ON Opciones.producto = calespinf.producto
					GROUP BY calespinf.producto, calespinf.especificacion, calespinf.informante
					PIVOT calespinf.periodo;"+
				*/
			);
			System.Console.WriteLine("Cálculo de ponderadores");
			repo.CalcularPonderadores("A");
			string periodoAnterior;
			string periodoActual;
			/*
			periodoAnterior="null";
			for(int mes=4;mes<=DateTime.Today.Month;mes++){
				for(int semana=1;semana<=4;semana++){
					periodoActual="'20080"+mes.ToString()+semana.ToString()+"'";
					db.EjecutrarSecuencia(
						@"INSERT INTO periodos VALUES ("+periodoActual+",2008,"+mes.ToString()+","+semana.ToString()+@");"+
						@"INSERT INTO calculos VALUES ("+periodoActual+",0,'N',"+periodoAnterior+")"
					);
					periodoAnterior=periodoActual;
				}
			}*/
			System.Console.WriteLine("Detectar periodos");
			db.EjecutrarSecuencia(
				@"INSERT INTO periodos (ano,mes,periodo) VALUES (0,0,'a0000m00');"+
				@"INSERT INTO periodos (ano,mes,periodo) SELECT ano,mes,'a' & ano & 'm' & iif(mes<10,'0' & mes, mes) FROM PreciosImportados WHERE mes>=2 and ano=2008 GROUP BY ano,mes;"+
				@"INSERT INTO calculos (periodo,calculo,esperiodobase) SELECT periodo,0,'N' FROM periodos;"+
				@"UPDATE calculos SET periodoanterior=DMAX('periodo','calculos','periodo<''' & periodo & ''' AND calculo=0') WHERE calculo=0;"+
				@"INSERT INTO NovEspInf (periodo,calculo,producto,especificacion,informante,estado)"+
				@" SELECT 'a0000m00',0,'P' & producto,observacion,informante,'Alta' FROM NovEspInf2008 IN '"+param.NombreBaseImportacion+"' WHERE estado='N' AND ano=2008 and mes=2 GROUP BY producto,observacion,informante;"+
				@"INSERT INTO ProdTipoInf (producto,tipoinformante,ponderadorTI) SELECT 'P' & producto, 'S', PonderadorSupermercado/100 FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO ProdTipoInf (producto,tipoinformante,ponderadorTI) SELECT 'P' & producto, 'T', (100-PonderadorSupermercado)/100 FROM productos IN '"+param.NombreBaseImportacion+"'"
			);
			/*
			{
				periodoAnterior="null";
				int mes=4;
				for(int semana=4;semana>=1;semana--){
					periodoActual="'a2008m0"+mes.ToString()+"s"+semana.ToString()+"'";
					db.EjecutrarSecuencia(
						@"INSERT INTO calculos VALUES ("+periodoActual+",-1,'S',"+periodoAnterior+")"
					);
					periodoAnterior=periodoActual;
				}
			}
			*/
			System.Console.WriteLine("Importando Precios");
			{
			db.EjecutrarSecuencia(
				@"CREATE INDEX PreciosImportados_ind ON PreciosImportados (ano,mes,producto,informante);"+
				@"CREATE INDEX PreciosImportados_ind2 ON PreciosImportados (producto,informante,ano,mes);"+
				@"CREATE UNIQUE INDEX periodos_ind ON periodos (ano,mes);"+
				@"INSERT INTO RelVar 
				    SELECT p.periodo, v.producto, v.especificacion, v.variedad, r.informante, max(r.precio) as precio
				      FROM periodos p,
				      	[preciosimportados] r,
				      	informantes i,
				      	variedades v
				      WHERE p.ano=r.ano
				        AND p.mes=r.mes
				        AND r.producto=v.producto
				        AND r.especificacion=v.especificacion
				        AND r.variedad=v.variedad
				        AND r.informante=i.informante
				        AND r.precio>0
				      GROUP BY r.ano,r.mes,r.producto, r.informante,p.periodo, v.producto, v.especificacion, v.variedad;
				  INSERT INTO RelVar (periodo, producto, especificacion, variedad, informante, precio) SELECT 'a0000m00', producto, especificacion, variedad, informante, precio FROM RelVar WHERE periodo='a2008m02'
				"
			);
			}
			// repo.CalcularMatrizBase(2);
			Calculos cal0=new Calculos();
			cal0.cPeriodo.Valor="a0000m00";
			cal0.cCalculo.Valor=0;
			repo.CalcularPreciosPeriodo(cal0,false);
			Calculos cals=new Calculos();
			/*
			cals.cPeriodo.Valor="a2008m02";
			cals.cCalculo.Valor=0;
			*/
			db.EjecutrarSecuencia(
				// @"INSERT INTO CalProd (periodo,calculo,producto,promedioprod) SELECT 'a0000m00',calculo,producto,promedioprod FROM CalProd WHERE periodo='a2008m02' and calculo=0;"+
				@"INSERT INTO CalGru (periodo,calculo,agrupacion,grupo,indice,factor) SELECT 'a0000m00',0,'A',iif(nivel=0,'A',iif(nivel=6,'P' & grupo,'A' & grupo)),indice,1 FROM CalGru2008 IN '"+param.NombreBaseImportacion+"' WHERE ano=2008 and mes=2 and version=0"
			);
			string ultimoCodigoPeriodo="";
			Agrupaciones agrupacion=new Agrupaciones();
			agrupacion.Leer(repo.db,"A");
			foreach(Calculos cal in new Calculos().Algunos(db,cals.cEsPeriodoBase.Igual(false).And(cals.cPeriodo.Distinto("a0000m00")))){
				repo.CalcularPreciosPeriodo(cal,true);
				repo.CalcularCalGru(cal,agrupacion);
				ultimoCodigoPeriodo=cal.cPeriodo.Valor;
			}
			System.Console.WriteLine("Control de integridad");
			repo.ReglasDeIntegridad();
			db.Close();
		}
		public void Generar(){
			DateTime Empezo=DateTime.Now;
			System.Console.WriteLine("Empezo "+Empezo.ToShortTimeString());
			ArmarBase();
			System.Console.WriteLine("Empezo "+Empezo.ToShortTimeString());
			System.Console.WriteLine("Termino "+DateTime.Now.ToShortTimeString());
			TimeSpan Tardo=DateTime.Now-Empezo;
			System.Console.WriteLine("Tardo "+Tardo.ToString());
			System.Console.WriteLine("Tardo "+(DateTime.Today+Tardo).ToShortTimeString());
		}
	}
}
