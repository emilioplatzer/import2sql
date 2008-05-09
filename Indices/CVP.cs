/*
 * Creado por SharpDevelop.
 * Usuario: ipcsl2
 * Fecha: 09/04/2008
 * Hora: 11:52
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;
using ModeladorSql;
using DelOffice;

namespace Indices
{
	public class CVP
	{
		/*
		public class CampoProducto:CampoChar{ public CampoProducto():base(4){} };
		public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
		*/
		public class CampoAtributo:CampoChar{ public CampoAtributo():base(250){} };
		public class CampoValor:CampoChar{ public CampoValor():base(250){} };
		#pragma warning disable 169
		public class FormulariosImportados:Tabla{
			[Pk] CampoEntero cAno;
			[Pk] CampoEntero cMes;
			CampoEntero cRazon;
			[Pk] CampoEntero cInformante;
			[Pk] CampoProducto cProducto;
			CampoNombre	cNombre;
			[Pk] CampoEntero cObservacion;
			[Pk] CampoAtributo cAtributo;
			CampoValor cValor;
		}
		#pragma warning restore 169
		public CVP()
		{
		}
		public void CreacionTablas(){
			BaseDatos db=BdAccess.Abrir(@"c:\cvp\pruebas\importaciones.mdb");
			FormulariosImportados f=new FormulariosImportados();
			db.ExecuteNonQuery(f.SentenciaCreateTable(db));
		}
		public void LevantarExcels(){
			
		}
	}
	#if SinOffice
	#else
	public class ProcesoLevantarPlanillasCVP{
		BaseDatos db;
		ReceptorSql receptor;
		public bool LevantarPlanilla(string nombreArchivo){
			receptor=new ReceptorSql(db,"formulariosimportados");
			MatrizExcelASql matriz=new MatrizExcelASql(receptor);
			LibroExcel libro=LibroExcel.Abrir(nombreArchivo);
			matriz.GuardarErroresEn=@"c:\cvp\temp\ErroresDeImportacion.sql";
			if(libro.TextoCelda("A1")=="FORM.PREC"){
				int fila=7;
				int columna=6;
				int filaFin=libro.BuscarPorColumnas("FIN!").NumeroFila-1;
				int columnaFin=libro.BuscarPorFilas("FIN!").NumeroColumna-1;
				string[] camposFijos=new string[]{"formato","origen","fecha_importacion",""};
				object[] valoresFijos=new object[]{libro.TextoCelda("A1"),nombreArchivo,DateTime.Now,null};
				libro.Rango("A3:A3").TextoRango1D().CopyTo(camposFijos,3);
				libro.Rango("B3:B3").ValorRango1D().CopyTo(valoresFijos,3);
				matriz.CamposFijos=Objeto.Paratodo(camposFijos,Cadena.Simplificar);
				matriz.ValoresFijos=valoresFijos;
				matriz.BuscarFaltantes=true;
				matriz.PasarHoja(libro.Rango(fila,columna,filaFin,columnaFin)
				                 ,libro.Rango(fila,1,filaFin,columna-2)
				                 ,libro.Rango(3,columna,fila-2,columnaFin)
				                 ,"valor"
				                 ,Objeto.Paratodo(libro.Rango(fila-1,1,fila-1,columna-2).TextoRango1D(),Cadena.Simplificar)
				                 ,Objeto.Paratodo(libro.Rango(3,columna-1,fila-2,columna-1).TextoRango1D(),Cadena.Simplificar));
			}else{
				System.Console.Write(" no es un formato valido reconocido");
				return false;
			}
			libro.CerrarNoHayCambios();
			return true;
		}
		public void TraerPlanillasRecepcion(){
			Carpeta dir=new Carpeta(@"c:\cvp\datos\ParaImportar\");
			db=BdAccess.Abrir(@"c:\cvp\pruebas\Importaciones.mdb");
			dir.ProcesarArchivos("*.xls","procesado",LevantarPlanilla);
		}
	}
	#endif
}
