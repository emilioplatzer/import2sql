/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 15/04/2008
 * Hora: 11:34
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using Modelador;

namespace Tareas
{
	public class TablaTmp:Tabla{
		public TablaTmp()
			:base()
		{
			NombreTabla="tmp_cp_"+NombreTabla;
		}
	}
	public class ComparacionPadrones:Repositorio{
		/*
		[Vista]
		class EntradaCallesViejas{
			
		}
		class EntradaCallesNuevas{
			
		}
		*/
		public class CampoConjunto:CampoChar{ public CampoConjunto():base(10){} };
		public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
		public class CampoMetodo:CampoChar{ public CampoMetodo():base(10){} };
		public class CampoPalabra:CampoChar{ public CampoPalabra():base(10){} };
		public class Nombres:TablaTmp{
			[Pk] public CampoConjunto cConjunto;
			[Pk] public CampoNombre cNombre;
			public CampoNombre cNombreReordenado;
		}
		public class Asignacion:TablaTmp{
			[Pk] public CampoNombre cNombreViejo;
			[Pk] public CampoNombre cNombreNuevo;
			public CampoMetodo cMetodo;
		}
		public class NombresPalabras:TablaTmp{
			[Pk] public CampoNombre cNombre;
			[Pk] public CampoEntero cOrden;
			public CampoPalabra cPalabra;
		}
		public ComparacionPadrones(BaseDatos db)
			:base(db)
		{
		}
		public void AsignacionExacta(){
			Asignacion a=new Asignacion();
			Nombres n=new Nombres();
			Nombres v=new Nombres();
			using(Ejecutador ej=new Ejecutador(db)){
				ej.Ejecutar(
					new SentenciaInsert(a)
						.Select(a.cNombreViejo.Es(v.cNombre),a.cNombreNuevo.Es(n.cNombre),a.cMetodo.Es("exacta"))
						.Where(v.cNombre.Igual(n.cNombre).And(v.cConjunto.Igual("viejo")).And(n.cConjunto.Igual("nuevo")))
				);
			}
		}
		public void SepararPalabras(){
			/*
			Nombres n=new Nombres();
			Palabras p=new NombresPalabras();
			Numeros num=new Numeros();
			*/
			
		}
	}
	[TestFixture]
	public class PrComparacionPadrones{
		BaseDatos db;
		ComparacionPadrones cp;
		public PrComparacionPadrones(){
			#pragma warning disable 162
			switch(3){ // Solo se va a tomar un camino
				case 1:
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					cp=new ComparacionPadrones(db);
					cp.EliminarTablas();
					cp.CrearTablas();
					break;
				case 3:
					string nombreBase="comparaciones.mdb";
					Archivo.Borrar(nombreBase);
					BdAccess.Crear(nombreBase);
					db=BdAccess.Abrir(nombreBase);
					cp=new ComparacionPadrones(db);
					cp.CrearTablas();
					break;
			}
			#pragma warning restore 162
		}
		public void Cargar1(string[] datos,string conjunto){
			ComparacionPadrones.Nombres n=new ComparacionPadrones.Nombres();
			foreach(string dato in datos){
				using(Insertador ins=new Insertador(db,n)){
					n.cConjunto[ins]=conjunto;
					n.cNombre[ins]=dato;
				}
			}
		}
		public void Cargar(string[] viejos,string[] nuevos){
			Cargar1(viejos,"viejo");
			Cargar1(nuevos,"nuevo");
		}
		public void CompararCoincidencias(string[] viejosCoincidentes,string[] nuevosCoincidentes){
			int item=0;
			foreach(ComparacionPadrones.Asignacion a in new ComparacionPadrones.Asignacion().Todos(db)){
				Assert.AreEqual(viejosCoincidentes[item],a.cNombreViejo.Valor,"viejos coindicentes "+item);
				Assert.AreEqual(nuevosCoincidentes[item],a.cNombreNuevo.Valor,"nuevos coindicentes "+item);
				item++;
			}
			Assert.AreEqual(viejosCoincidentes.Length,item,"Debe coincidir la cantidad");
		}
		[Test]
		public void PrExacta(){
			string[] viejos={"San Martín","Belgrano","Corrientes","No esta"," distinta "};
			string[] nuevos={"San Martín","Belgrano","Corrientes","Tampoco esta","distinta"};
			string[] coinciden={"San Martín","Belgrano","Corrientes"};
			Cargar(viejos,nuevos);
			cp.AsignacionExacta();
			CompararCoincidencias(coinciden,coinciden);
			cp.SepararPalabras();
		}
	}
}
