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
// using CampoEntero=Modelador.CampoTipo<int>;
// using CampoPkEntero=Modelador.CampoPkTipo<int>;
// using CampoPkChar=Modelador.CampoPkTipo<string>;
using TodoASql;
using Modelador;
using Indices;

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
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				c.Nombre=m.Name.Substring(1);
      				c.NombreCampo=c.Nombre.ToLowerInvariant();
      				m.SetValue(this,c);
      				foreach (System.Attribute attr in m.GetCustomAttributes(true)){
      					if(attr is AplicadorCampo){
      						AplicadorCampo apl=attr as AplicadorCampo;
      						apl.Aplicar(ref c);
      					}
      				}
				}
			}
		}
		protected void Construir(){
			ConstruirCampos();
		}
		public string SentenciaCreateTable(){
			StringBuilder rta=new StringBuilder();
			StringBuilder pk=new StringBuilder("primary key (");
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
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
	public class CampoEntero:Modelador.CampoTipo<int>{};
	// public class CampoPkEntero:Modelador.CampoPkTipo<int>{};
	public class CampoChar:Modelador.CampoTipo<string>{
		public int Largo;
		protected CampoChar(int largo){
			this.Largo=largo;	
		}
		public override string TipoCampo{ 
			get { return "varchar("+Largo.ToString()+")"; }
		}
	};
	/*
	public class CampoPkChar:Modelador.CampoPkTipo<string>{};
	public class CampoPk:Campo{
		public CampoPk(){
			this.EsPk=true;	
		}
		public override string TipoCampo {
			get { Assert.Fail("CampoPk no es real"); return null; }
		}
	}
	*/
	public abstract class AplicadorCampo:System.Attribute{
	   	public abstract void Aplicar(ref Campo campo);
	}
	public class Pk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	   		campo.EsPk=true;
	    }
	}
}
namespace PrModelador
{
	public class Periodos:Tabla{
		[Pk] public CampoEntero cAno;
		[Pk] public CampoEntero cMes;
		public CampoEntero cAnoAnt;
		public CampoEntero cMesAnt;
	}
	[TestFixture]
	public class prTabla{
		public prTabla(){
		}
		class Productos:Tabla{
			[Pk] CampoProducto cProducto;
			CampoNombre	cNombreProducto;
		}
		[Test]
		public void Periodos(){
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(ano integer,mes integer,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable()));
			Productos pr=new Productos();
			Assert.AreEqual("create table productos(producto varchar(4),nombreproducto varchar(250),primary key(producto));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable()));
		}
	}
}
