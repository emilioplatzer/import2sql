/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 04/05/2008
 * Hora: 12:56
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public abstract class Sentencia{
		Instruccion instruccion;
		protected Sentencia(Instruccion instruccion){
			// Sí o sí hay que crear una instrucción y pasar el puntero acá
			this.instruccion=instruccion;
		}
		public virtual string ToSql(BaseDatos db){
			return instruccion.ToSql(db);
		}
		
	}
	public class SentenciaInsert:Sentencia{
		InstruccionInsert instruccionInsert;
		public SentenciaInsert(Tabla TablaBase)
			:base(instruccionInsert=new InstruccionInsert())
		{
			instruccionInsert.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params IConCampos[] conCampos){
			instruccionInsert.InstruccionSelectBase.ClausulaSelect=conCampos;
		}
				
	}
}
