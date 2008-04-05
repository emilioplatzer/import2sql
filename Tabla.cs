/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 04/04/2008
 * Time: 05:38 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using CampoEntero=Modelador.CampoTipo<int>;
using CampoPkEntero=Modelador.CampoPkTipo<int>;
using TodoASql;
using Modelador;

namespace Modelador
{
	/// <summary>
	/// Description of Tabla.
	/// </summary>
	public abstract class Tabla
	{
		public string NombreTabla;
		public Tabla()
		{
			Construir();
			NombreTabla=this.GetType().Name.ToLowerInvariant();
		}
		protected virtual void ConstruirCampos(){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(/*System.Reflection.BindingFlags.NonPublic*/);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=m.Name;
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				m.SetValue(this,c);
				}
			}
		}
		protected void Construir(){
			ConstruirCampos();
		}
		public string SentenciaCreateTable(){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("primary key (");
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(/*System.Reflection.BindingFlags.NonPublic*/);
			rta.AppendLine("create table "+this.NombreTabla+"(");
      		Separador comapk=new Separador(",");
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
					Campo c=(Campo)m.GetValue(this);
					rta.AppendLine("\t"+c.NombreCampo+" "+c.TipoCampo+",");
					if(c.EsPk){
						pk.Append(comapk+c.NombreCampo);
					}
				}
			}
      		pk.AppendLine(")");
      		rta.Append(pk);
			rta.AppendLine(");");
			return rta.ToString();
		}
	}
	public abstract class Campo 
	{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		public Campo(){
		}
	}
	public class CampoTipo<T>:Campo{
		public T valor;
		public override string TipoCampo{ 
			get {
				if(valor is int){
					return "integer";
				}else if(valor is string){
					return "varchar";
				}else{
					return typeof(T).Name; 
				}
			} 
		}
		public CampoTipo()
		{	
		}
	}
	public class CampoPkTipo<T>:CampoTipo<T>{
		public CampoPkTipo()
		{	
			EsPk=true;
		}
	}
}
namespace PrModelador
{

	public class Periodos:Tabla{
		public CampoPkEntero cAno;
		public CampoPkEntero cMes;
		public CampoEntero cAnoAnt;
		public CampoEntero cMesAnt;
	}
	[TestFixture]
	public class prTabla{
		public prTabla(){
		}
		[Test]
		public void Periodos(){
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.valor);
			Assert.AreEqual("cAno",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(cano integer,cmes integer,canoant integer,cmesant integer,primary key(cano,cmes));", 
			                Cadena.Simplificar(p.SentenciaCreateTable()));
		}
	}
}
