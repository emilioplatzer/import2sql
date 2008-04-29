/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 05/04/2008
 * Time: 12:48 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;

using Comunes;
using BasesDatos;

namespace Modelador
{
	public class Repositorio
	{
		internal BaseDatos db;
		public Repositorio(BaseDatos db){
			this.db=db;
		}
		public static void CrearTabla(Assembly assem,BaseDatos db,Type t){
			bool crear=true;
			foreach(System.Attribute attr in t.GetCustomAttributes(true)){
				if(attr is Vista){
					crear=false;
				}
			}
			if(crear){
				Tabla tabla=(Tabla)assem.CreateInstance(t.FullName);
				db.ExecuteNonQuery(tabla.SentenciaCreateTable(db));
			}
		}
		public static void CrearTablas(BaseDatos db,string NombreNamespace){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Type[] ts=assem.GetExportedTypes();
			foreach(Type t in ts){
      			if(t.Namespace==NombreNamespace){
					if(t.IsSubclassOf(typeof(Tabla))){
						CrearTabla(assem,db,t);
					}
      			}
			}
		}
		public virtual void CrearTablas(){
      		Assembly assem = Assembly.GetExecutingAssembly();
			System.Type[] ts=this.GetType().GetNestedTypes();
			foreach(Type t in ts){
				if(t.IsSubclassOf(typeof(Tabla))){
					CrearTabla(assem,db,t);
				}
			}
		}
		private static void RegistrarParaEliminarTabla(Assembly assem,System.Collections.Generic.Stack<string> NombresTablasABorrar,Type t){
			bool borrar=true;
			foreach(System.Attribute attr in t.GetCustomAttributes(true)){
				if(attr is Vista){
					borrar=false;
				}
			}
			if(borrar){
				Tabla tabla=(Tabla)assem.CreateInstance(t.FullName);
				NombresTablasABorrar.Push(tabla.NombreTabla);
			}
		}
		public virtual void EliminarTablas(){
			System.Collections.Generic.Stack<string> NombresTablasABorrar=new System.Collections.Generic.Stack<string>();
      		Assembly assem = Assembly.GetExecutingAssembly();
			System.Type[] ts=this.GetType().GetNestedTypes();
			foreach(Type t in ts){
				if(t.IsSubclassOf(typeof(Tabla))){
					RegistrarParaEliminarTabla(assem,NombresTablasABorrar,t);
				}
			}
			foreach(string nombreTabla in NombresTablasABorrar){
				db.EliminarTablaSiExiste(nombreTabla);
			}
		}
		public virtual void EliminarTablas(BaseDatos db,string NombreNamespace){
			System.Collections.Generic.Stack<string> NombresTablasABorrar=new System.Collections.Generic.Stack<string>();
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Type[] ts=assem.GetExportedTypes();
			foreach(Type t in ts){
      			if(t.Namespace==NombreNamespace){
					if(t.IsSubclassOf(typeof(Tabla))){
						RegistrarParaEliminarTabla(assem,NombresTablasABorrar,t);
      				}
      			}
			}
			foreach(string nombreTabla in NombresTablasABorrar){
				db.EliminarTablaSiExiste(nombreTabla);
			}
		}
	}
}
