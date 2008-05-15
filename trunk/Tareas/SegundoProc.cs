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
			param=new ParametrosPruebasExternas(Parametros.LeerPorDefecto.SI,"repo");
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
			ProcesoLevantarPlanillas.CrearTablaReceptora(db);
			System.Console.WriteLine("Importando codigos");
			db.EjecutrarSecuencia(
				@"INSERT INTO tipoinf (tipoinformante,otrotipoinformante) VALUES ('S','T');"+
				@"INSERT INTO tipoinf (tipoinformante,otrotipoinformante) VALUES ('T','S');"+
				@"INSERT INTO productos (producto,nombreproducto) "+
				@" SELECT 'P' & producto,nombreproducto FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO especificaciones (producto,especificacion,nombreespecificacion,tamannonormal) SELECT 'P' & producto,1,especificacion,null FROM productos IN '"+param.NombreBaseImportacion+"';"+
				@"INSERT INTO variedades (producto,especificacion,variedad,nombrevariedad,tamanno,unidad) SELECT 'P' & producto,1,1,null,null,null FROM Variedades IN '"+param.NombreBaseImportacion+"';"+
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
				@"INSERT INTO preciosImportados SELECT * FROM PreciosImportados IN '"+param.NombreBaseImportacion+"';"+
				@"CREATE VIEW [Control Codigos Informante] AS
					SELECT PreciosImportados.Cod_Info, PreciosImportados.Origen
					FROM PreciosImportados LEFT JOIN informantes ON PreciosImportados.Cod_Info = informantes.informante
					WHERE (((informantes.informante) Is Null))
					GROUP BY PreciosImportados.Cod_Info, PreciosImportados.Origen;"+
				@"CREATE VIEW [Control Nombres Informante] AS
					SELECT PreciosImportados.Cod_Info, PreciosImportados.Informante, informantes.nombreinformante, informantes.rubro, informantes.cadena, informantes.direccion, PreciosImportados.Origen
					FROM informantes INNER JOIN PreciosImportados ON informantes.informante = PreciosImportados.Cod_Info
					GROUP BY PreciosImportados.Cod_Info, PreciosImportados.Informante, informantes.nombreinformante, informantes.rubro, informantes.cadena, informantes.direccion, PreciosImportados.Origen;"+
				@"CREATE VIEW [Control Codigos Variedad] AS
					SELECT PreciosImportados.Cod_Var, PreciosImportados.Nombre, PreciosImportados.Especificacion, PreciosImportados.Variedad, PreciosImportados.Origen
					FROM PreciosImportados LEFT JOIN variedades ON PreciosImportados.Cod_Var = variedades.codigovariedad
					WHERE (((variedades.variedad) Is Null))
					GROUP BY PreciosImportados.Cod_Var, PreciosImportados.Nombre, PreciosImportados.Especificacion, PreciosImportados.Variedad, PreciosImportados.Origen;"+
				@"CREATE TABLE Opciones (producto varchar(4), foreign key (producto) references productos (producto))"+
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
				@""
			);
			System.Console.WriteLine("Cálculo de ponderadores");
			repo.CalcularPonderadores("C");
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
				@"INSERT INTO periodos (ano,mes,semana,periodo) VALUES (0,0,9,'a0000m00');"+
				@"INSERT INTO periodos (ano,mes,semana,periodo) SELECT ano,mes,semana,'a' & ano & 'm' & iif(mes<10,'0' & mes, mes) & 's' & semana FROM PreciosImportados WHERE semana>0 AND mes>=4 and ano=2008 GROUP BY ano,mes,semana;"+
				@"INSERT INTO calculos (periodo,calculo,esperiodobase) SELECT periodo,0,'N' FROM periodos;"+
				@"UPDATE calculos SET periodoanterior=DMAX('periodo','calculos','periodo<''' & periodo & ''' AND calculo=0') WHERE calculo=0"
			);
			/*+
				@"UPDATE calculos SET periodoanterior='a0000m00' WHERE calculo=0 AND periodo='a2008m04s1'"
			 */
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
			System.Console.WriteLine("Importando Precios");
			{
			db.EjecutrarSecuencia(
				@"CREATE INDEX PreciosImportados_ind ON PreciosImportados (ano,mes,semana,cod_var,cod_info);"+
				@"CREATE INDEX PreciosImportados_ind2 ON PreciosImportados (cod_var,cod_info,ano,mes,semana);"+
				@"CREATE UNIQUE INDEX Variedades_ind ON Variedades (codigovariedad);"+
				@"CREATE UNIQUE INDEX periodos_ind ON periodos (ano,mes,semana);"+
				@"INSERT INTO RelVar 
				    SELECT p.periodo, v.producto, v.especificacion, v.variedad, r.cod_info as informante, max(r.precio) as precio
				      FROM periodos p,
				      	[preciosimportados] r,
				      	informantes i,
				      	variedades v
				      WHERE p.ano=r.ano
				        AND p.mes=r.mes
				        AND p.semana=r.semana
				        AND r.cod_var=v.codigovariedad
				        AND r.cod_info=i.informante
				        AND r.precio>0
				        AND r.semana>0
				        AND r.mes>3
				        AND r.mes<10
				        AND (r.cod_var IN ('P102','P180','P332','P391','P495') OR 1=1)
				      GROUP BY r.ano,r.mes,r.semana,r.cod_var, r.cod_info,p.periodo, v.producto, v.especificacion, v.variedad
				"
			);
			}
			repo.CalcularMatrizBase(2);
			Calculos cals=new Calculos();
			string ultimoCodigoPeriodo="";
			Agrupaciones agrupacion=new Agrupaciones();
			agrupacion.Leer(repo.db,"C");
			foreach(Calculos cal in new Calculos().Algunos(db,cals.cEsPeriodoBase.Igual(false).And(cals.cPeriodo.Distinto("a0000m00")))){
				repo.CalcularPreciosPeriodo(cal,true);
				repo.CalcularCalGru(cal,agrupacion);
				ultimoCodigoPeriodo=cal.cPeriodo.Valor;
			}
			System.Console.WriteLine("Control de integridad");
			repo.ReglasDeIntegridad();
			db.Close();
		}
	}
}
