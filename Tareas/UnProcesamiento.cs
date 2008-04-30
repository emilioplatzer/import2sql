/*
 * Creado por SharpDevelop.
 * Usuario: asaccal
 * Fecha: 19/03/2008
 * Hora: 14:02
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;
using DelOffice;
using Indices;

#if SinOffice
#else
namespace Tareas
{
	public class UnProcesamiento
	{
		static LibroExcel codigos;
		static string carpeta=@"c:\temp\entrada\";
		public UnProcesamiento()
		{
		}
		public static void ProcesarUno(string nombreArchivoXLS, int anno, int mes){
			ParametrosMatrizExcelASql parametros=new ParametrosMatrizExcelASql(carpeta+"PreciosHistoricos.mdb","Precios");
			ReceptorSql receptor=new ReceptorSql(parametros);
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(carpeta+nombreArchivoXLS);
			matriz.CamposFijos=new string[]{"ano","mes"};
			matriz.ValoresFijos=new object[]{anno,mes};
			matriz.PasarHoja(
				libro.Rango("K5","BJ315"),
				new RangoExcel[]{
					codigos.Rango("A5","A315"),
					libro.Rango("I5","I315"),
				},new RangoExcel[]{
					libro.Rango("K2","BJ2"),
					libro.Rango("K3","BJ3")
				},
				"precio",
				new string[]{
					"producto","unidad"
				},
				new string[]{
					"informante","complemento_informante"
				}
			);
			libro.CerrarNoHayCambios();
		}
		public static void Ahora(){
			string[] nombreMeses={
				"Abr06.xls",
				"Ago06.xls",
				"Dic06.xls",
				"Ene06.xls",
				"Feb06.xls",
				"Jul06.xls",
				"Jun06.xls",
				"Mar06.xls",
				"May06.xls",
				"Nov06.xls",
				"Oct06.xls",
				"Sep06.xls"
			};
			int [] numeroMeses={4,8,12,1,2,7,6,3,5,11,10,9};
			codigos=LibroExcel.Abrir(carpeta+"Codigos.xls" );
			for(int i=0;i<12;i++){
				ProcesarUno(nombreMeses[i],2006,numeroMeses[i]);
			}
			codigos.CerrarNoHayCambios();
		}
	}
	public class ParametrosPruebasExternas:Parametros{
		public string NombreBase;
		public string NombreBaseCodigos;
		public string NombreBaseImportacion;
		public ParametrosPruebasExternas(LeerPorDefecto queHacer,string N):base(queHacer,N){}	
	}
	public class PruebasExternas{
		ParametrosPruebasExternas param;
		BdAccess db;
		RepositorioIndice repo;
		public PruebasExternas(){
			param=new ParametrosPruebasExternas(Parametros.LeerPorDefecto.SI,"repo");
		}
		public void AbrirBase(){
			db=BdAccess.Abrir(param.NombreBase);
			repo=new RepositorioIndice(db);
		}
		public void ArmarBase(){
			System.Console.WriteLine("Recreando la base");
			Archivo.Borrar(param.NombreBase);
			BdAccess.Crear(param.NombreBase);
			AbrirBase();
			repo.CrearTablas();
			ProcesoLevantarPlanillas.CrearTablaReceptora(db);
			System.Console.WriteLine("Importando codigos");
			db.EjecutrarSecuencia(
				@"INSERT INTO productos (producto,nombreproducto) "+
				@"SELECT codigoproducto,nombreproducto FROM productos IN '"+param.NombreBaseCodigos+"';"+
				@"INSERT INTO especificaciones (especificacion,nombreespecificacion,tamannonormal,producto) SELECT codigoespecificacion,descripcionespecificacion,tamannonormal,codigoproducto FROM especificaciones IN '"+param.NombreBaseCodigos+"';"+
				@"INSERT INTO variedades (variedad,nombrevariedad,tamanno,unidad,especificacion) SELECT codigovariedad,descripcionvariedad,tamanno,unidad,codigoespecificacion FROM variedades IN '"+param.NombreBaseCodigos+"';"+
				@"INSERT INTO agrupaciones (agrupacion) VALUES ('C');"+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) VALUES ('C','C','Nivel General',null,100,0,'N'); "+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) "+
				@"SELECT 'C','C' & CodigoOriginal,NombreOriginal,'C' & left(CodigoOriginal,nivel-1),ponderadordic2006,nivel,'N' "+
				@" FROM CanastaOriginal IN '"+param.NombreBaseCodigos+"' WHERE nivel<6 AND nivel>0 ORDER BY nivel,CodigoOriginal;"+
				@"INSERT INTO grupos (agrupacion,grupo,nombregrupo,grupopadre,ponderador,nivel,esproducto) "+
				@"SELECT 'C',producto,null,'C' & padre,PondProd,6,'S' "+
				@" FROM PonderadoresDeProductos IN '"+param.NombreBaseCodigos+"';"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"delete * from grupos where DCount('agrupacion','grupos','agrupacion=''' & agrupacion & ''' and grupopadre=''' & grupo & '''')=0 and nivel<=5;"+
				@"INSERT INTO informantes SELECT * FROM informantes IN '"+param.NombreBaseCodigos+"';"+
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
					FROM PreciosImportados LEFT JOIN variedades ON PreciosImportados.Cod_Var = variedades.variedad
					WHERE (((variedades.variedad) Is Null))
					GROUP BY PreciosImportados.Cod_Var, PreciosImportados.Nombre, PreciosImportados.Especificacion, PreciosImportados.Variedad, PreciosImportados.Origen
					"
			);
			string periodoAnterior;
			string periodoActual;
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
			}
			{
				periodoAnterior="null";
				int mes=4;
				for(int semana=4;semana>=1;semana--){
					periodoActual="'20080"+mes.ToString()+semana.ToString()+"'";
					db.EjecutrarSecuencia(
						@"INSERT INTO calculos VALUES ("+periodoActual+",-1,'S',"+periodoAnterior+")"
					);
					periodoAnterior=periodoActual;
				}
			}
			System.Console.WriteLine("Importando Precios");
			{
			db.EjecutrarSecuencia(
				@"CREATE INDEX PreciosImportados_ind ON PreciosImportados (ano,mes,variedad,informante);"+
				@"CREATE INDEX PreciosImportados_ind2 ON PreciosImportados (variedad,informante,ano,mes);"+
				@"CREATE UNIQUE INDEX periodos_ind ON periodos (ano,mes,semana);"+
				@"INSERT INTO RelVar 
				    SELECT p.periodo, r.cod_var as variedad, r.cod_info as informante, max(r.precio) as precio
				      FROM periodos p,
				      	[preciosimportados] r,
				      	informantes i,
				      	variedades v
				      WHERE p.ano=r.ano
				        AND p.mes=r.mes
				        AND p.semana=r.semana
				        AND r.cod_var=v.variedad
				        AND r.cod_info=i.informante
				        AND r.precio>0
				        AND r.semana>0
				        AND r.mes>3
				        AND r.mes<10
				        AND r.cod_var IN ('P102','P180','P332','P391','P495')
				      GROUP BY p.periodo, r.cod_var, r.cod_info 
				"
			);
			}
			repo.CalcularMatrizBase(2);
			Calculos cals=new Calculos();
			string ultimoCodigoPeriodo="";
			foreach(Calculos cal in new Calculos().Algunos(db,cals.cEsPeriodoBase.Igual(false))){
				repo.CalcularPreciosPeriodo(cal);
				ultimoCodigoPeriodo=cal.cPeriodo.Valor;
			}
			db.Close();
		}
		public void Reponderar(){
			AbrirBase();
			repo.CalcularPonderadores("C");
			repo.ReglasDeIntegridad();
			db.Close();
		}
		public void ImportarPrecios(){
			
		}
		public void Generar(){
			ArmarBase();
			Reponderar();
		}
	}
	public class ProcesoLevantarPlanillas{
		BaseDatos db;
		ReceptorSql receptor;
		MatrizExcelASql matriz;
		LibroExcel libro;
		string NombreArchivo;
		string etiquetaFinal="FIN!";
		bool LevantarParametrica(int cantidadVertice, // o sea arriba a la izquierda como campos únicos
		                         int fila, int columna, // fila columna del primer dato 'util
		                         int primerFila, int primerColumna // primera fila y primera columna con datos (matriz completa sin el vertice)
		                        )
		{
			int filaFin=libro.BuscarPorColumnas(etiquetaFinal).NumeroFila-1;
			int columnaFin=libro.BuscarPorFilas(etiquetaFinal).NumeroColumna-1;
			if(filaFin<fila || columnaFin<columna){
				System.Console.Write(" problema con la etiqueta "+etiquetaFinal+
				                    " no deberia: "+filaFin+"<"+fila+" || "+columnaFin+"<"+columna);
				libro.CerrarNoHayCambios();
				return false;
			}
			string[] camposFijos=new string[]{"formato","origen","fecha_importacion"};
			object[] valoresFijos=new object[]{libro.TextoCelda("A1"),NombreArchivo,DateTime.Now};
			string[] camposFijosMasVertice=new string[camposFijos.Length+cantidadVertice];
			object[] valoresFijosMasVertice=new object[camposFijos.Length+cantidadVertice];
			camposFijos.CopyTo(camposFijosMasVertice,0);
			valoresFijos.CopyTo(valoresFijosMasVertice,0);
			if(cantidadVertice>0){
				libro.Rango(3,1,cantidadVertice+3-1,1).TextoRango1D().CopyTo(camposFijosMasVertice,camposFijos.Length);
				libro.Rango(3,2,cantidadVertice+3-1,2).ValorRango1D().CopyTo(valoresFijosMasVertice,camposFijos.Length);
			}
			matriz.CamposFijos=Objeto.Paratodo(camposFijosMasVertice,Cadena.Simplificar);
			matriz.ValoresFijos=valoresFijosMasVertice;
			// matriz.BuscarFaltantes=true;
			string[] TitulosFila=Objeto.Paratodo(libro.Rango(fila-1,primerColumna,fila-1,columna-2).TextoRango1D(),Cadena.Simplificar);
			if(TitulosFila[0]=="Cod_Prod"){
				TitulosFila[0]="Cod_Var";
			}
			matriz.PasarHoja(libro.Rango(fila,columna,filaFin,columnaFin)
			                 ,libro.Rango(fila,primerColumna,filaFin,columna-2)
			                 ,libro.Rango(primerFila,columna,fila-2,columnaFin)
			                 ,"precio"
			                 ,TitulosFila
			                 ,Objeto.Paratodo(libro.Rango(primerFila,columna-1,fila-2,columna-1).TextoRango1D(),Cadena.Simplificar));
			libro.CerrarNoHayCambios();
			return true;			
		}
		public bool LevantarPlanilla(string nombreArchivo){
			receptor=new ReceptorSql(db,"PreciosImportados");
			matriz=new MatrizExcelASql(receptor);
			libro=LibroExcel.Abrir(nombreArchivo);
			matriz.GuardarErroresEn=@"c:\temp\indice\Campo\Bases\ErroresDeImportacion.sql";
			matriz.InsertarValorCentral=InsertarValorCentralStringASennal;
			NombreArchivo=nombreArchivo;
			if(libro.TextoCelda("A1")=="PER/PROD/INF"){
				return LevantarParametrica(3,8,8,4,1);
			}else if(libro.TextoCelda("A1")=="PROD.INF/PER"){
				return LevantarParametrica(0,8,10,3,1);
			}else if(libro.TextoCelda("A1")=="PROD/PER.INF"){
				return LevantarParametrica(0,10,8,3,1);
			}else{
				libro.CerrarNoHayCambios();
				System.Console.Write(" no es un formato valido reconocido");
				return false;
			}
		}
		public void TraerPlanillasRecepcion(){
			ParametrosPruebasExternas param=new ParametrosPruebasExternas(Parametros.LeerPorDefecto.SI,"repo");
			Carpeta dir=new Carpeta(@"c:\temp\indice\Campo\RecepcionPura\");
			db=BdAccess.Abrir(param.NombreBaseImportacion);
			// CrearUnaTabla231(db);
			dir.ProcesarArchivos("*.xls","procesado",LevantarPlanilla);
		}
		public static void InsertarValorCentralStringASennal(InsertadorSql insert,string campoValor,object valor){
			if(valor is string){
				insert["sennal"+campoValor]=valor;
			}else{
				insert[campoValor]=valor;
			}
		}
		public static void CrearTablaReceptora(BaseDatos db){
			db.EjecutrarSecuencia(@"
				create table PreciosImportados(
				Ano integer,
				Mes integer,
				Semana integer,
				Cod_Info integer,
				Informante varchar(100),
				Fecha date,
				Cod_Var varchar(10),
				Nombre varchar(100),
				Especificacion varchar(250),
				Variedad varchar(250),
				Tamano varchar(100),
				Unidad varchar(100),
				Precio double precision,
				SennalPrecio varchar(100),
				Formato varchar(100),
				Origen varchar(250),
				Fecha_Importacion date)
			");
		}
	}
}
#endif
