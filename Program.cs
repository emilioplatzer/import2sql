/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:08 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */
 
using System;
using System.Reflection;

using Comunes;
using Indices;
using ModeladorSql;
using BasesDatos;
using Tareas;
using DelOffice;

namespace TodoASql
{
	public class ParametrosPrograma:Parametros{
		public string Contexto;
		public string Ejecutar;
		public ParametrosPrograma(LeerPorDefecto queHacer,string N):base(queHacer,N){}	
	}
	class Program
	{
		public static void Main(string[] args)
		{
			if(args.Length>0){
				string NombreArchivoParametros=args[0];
				// string NombreArchivoParametros="Automatica.programa";
				ParametrosPrograma param=new ParametrosPrograma(Parametros.LeerPorDefecto.SI,NombreArchivoParametros);
      			Assembly assem = Assembly.GetExecutingAssembly();
      			if(!Archivo.Existe(NombreArchivoParametros)){
      				System.Windows.Forms.MessageBox.Show("No existe el archivo "+NombreArchivoParametros);
      				return;
      			}
      			object o=assem.CreateInstance(param.Contexto);
      			o.GetType().InvokeMember(param.Ejecutar,BindingFlags.DeclaredOnly | 
            		BindingFlags.Public | BindingFlags.NonPublic | 
            		BindingFlags.Instance | BindingFlags.InvokeMethod,null,o,null);
				Console.WriteLine("Listo!");
				Console.Write("presione cualquier tecla . . . ");
				System.Windows.Forms.MessageBox.Show("Listo");
				// Console.ReadKey(true);
				return;
			}else{
				DateTime comenzo=DateTime.Now;
				Console.WriteLine("Comenzo "+DateTime.Now.ToLongTimeString());
				/*
				PruebasExternas p=new PruebasExternas();
				p.ArmarBase();
				p.Reponderar();
				*/
				// new Tareas.PrComparacionPadrones().PrExacta();
				// new probarLibroExcel().UltimoFilasyColumnas();
				// new ProcesoLevantarPlanillasCVP().TraerPlanillasRecepcion();
				// new CVP().CreacionTablas();
				// new prTabla().Periodos();
				// new PrModeladorSql.prModelador().SentenciaInsert();
				// new prTabla().UpdateSuma();
				// new prTabla().SentenciaCompuesta();
				// new PruebasReflexion().NombresMiembros();
			    // new ProbarSqLite().ConexionSqLite();
			    // new ProcesoLevantarPlanillas().TraerPlanillasRecepcion();
			    // RecolectorExcel.Ejemplo();
			    // new prRecolectarExcel().UnExcel();
			    /*
			    PrModelador.prTabla p=new PrModelador.prTabla();
			    p.FkConDatos();
			    p.G_Enumerados();
			    */
			    // new prTabla().SubselectGroupBy();
			    // new PrModeladorSql.prModelador().SentenciaInsert();
			    // new PrModeladorSql.prModelador().SentenciaUpdate();
			    // new PrModeladorSql.prModelador().SentenciaCompuesta();
			    // new PrModeladorSql.prModelador().UpdateSuma();
			    // new PrModeladorSql.prModelador().FkConDatos();
			    // new PrModeladorSql.prModelador().Subselect();
			    // new Tareas.PruebasExternas().Generar();
			    // new Tareas.SegundoProc().Generar();
			    // new ProbarMailASql().Proceso();
				//{
				//    var db=BdAccess.Abrir(@"c:\cvp\temp\Estructura.mdb");
				//	var repo=new RepositorioIndice(db);
				//	repo.CrearTablas();
				//
				//}
			    // new ProcesoLevantarPlanillasCVP().TraerPlanillasRecepcion();
			    /*
				Console.WriteLine("Listo!");
				Console.Write("presione cualquier tecla . . . ");
				System.Windows.Forms.MessageBox.Show("Listo");
				*/
				new PBG.PrCierrePBG();
				/*
				var t=new Tareas.PrComparacionPadrones();
				t.Pr01Exacta();
				t.Pr02ExtraccionPalabras();
				t.Pr03Reordenacion();
				// */
				// new PrModeladorSql.prModelador().Subselect();
			    /*
			    ProbarIndiceD3 p=new ProbarIndiceD3();
			    p.A01CalculosEstructuraBase();
			    p.A02CalculosMatrizBase();
			    p.A03CalculosMensuales();
			    p.VerCanasta();
			    p.zReglasDeIntegridad();
			    // */ 
				// new ProbarPostgreSql().Conexion();
				// new ProbarBdAccess().Creacion();
				// new ProbarMatrizExcelASqlGenerica().trasvasar();
				// new probarLibroExcel().Rango();
				// new ProbarFormulario().FormDerivado();
				// new ProbarMailASql().Proceso();
				// UnProcesamiento.Ahora();
				// /*
				/*
				#if SinOffice
				#else
				ProbarMatrizExcelASql p=new ProbarMatrizExcelASql();
				p.crear();
				p.revisar();
				p.crearReceptor();
				p.trasvasar();
				p.trasvasarConParametros();
				#endif
				// */
				// PruebaFormularios.Primero();
				// new Pruebas().Proceso(); /*
				/*
				PruebasParametros.MostrarVariablesDelSistema();
				try{
					new MailASql().LoQueSeaNecesario();
					System.Windows.Forms.MessageBox.Show("Listo!");
				}catch(System.Data.OleDb.OleDbException e){
					System.Windows.Forms.MessageBox.Show(
						e.Message
						,"Error en el manejo de la base de datos"
						,System.Windows.Forms.MessageBoxButtons.OK
						,System.Windows.Forms.MessageBoxIcon.Error
					);
				}catch(System.IO.DirectoryNotFoundException e){
					System.Windows.Forms.MessageBox.Show(
						e.Message
						,"Error, no encontró el directorio. "
						,System.Windows.Forms.MessageBoxButtons.OK
						,System.Windows.Forms.MessageBoxIcon.Error
					);
				
				} // */
				Console.WriteLine("Listo!");
				Console.WriteLine("Terminó "+DateTime.Now.ToLongTimeString());
				System.TimeSpan demoro=DateTime.Now-comenzo;
				Console.WriteLine("Demoró "+demoro.ToString());
				Console.Write("presione cualquier tecla . . . ");
				System.Windows.Forms.MessageBox.Show("Listo");
				// Console.ReadKey(true);
			}
		}
	}
}
